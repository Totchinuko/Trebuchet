using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using tot_lib;
using Trebuchet.Services;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.Panels;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

public sealed class TrebuchetApp : ReactiveObject
{
    public static string VersionHeader => ProcessUtil.GetStringVersion();
    
    public TrebuchetApp(
        AppSetup setup,
        AppFiles appFiles,
        Launcher launcher, 
        UIConfig uiConfig,
        Steam steam, 
        DialogueBox box,
        SteamWidget steamWidget,
        DialogueBox dialogueBox,
        OnBoarding onBoarding,
        IUpdater updater,
        IEnumerable<IPanel> panels)
    {
        _setup = setup;
        _appFiles = appFiles;
        _launcher = launcher;
        _steam = steam;
        _box = box;
        _onBoarding = onBoarding;
        _updater = updater;
        _panels = panels.ToList();
        SteamWidget = steamWidget;
        DialogueBox = dialogueBox;
        
        foreach (var panel in _panels)
        {
            var command = new PanelTab(panel);
            command.PanelSelected += OnTabClicked;
            
            if(panel is IRefreshingPanel refreshing)
                refreshing.RequestRefresh += (_,_) => RefreshPanels();
            
            if(panel is IBottomPanel)
                BottomPanels.Add(command);
            else
                TopPanels.Add(command);
        }

        _activePanel = BottomPanels.First(x => x.Panel.CanBeOpened);

        FoldedMenu = uiConfig.FoldedMenu;
        ToggleFoldedCommand = ReactiveCommand.Create(() =>
        {
            FoldedMenu = !FoldedMenu;
            uiConfig.FoldedMenu = FoldedMenu;
            uiConfig.SaveFile();
        });
            
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimerTick);

        Start();
    }
    
    private readonly AppSetup _setup;
    private readonly AppFiles _appFiles;
    private readonly Launcher _launcher;
    private readonly Steam _steam;
    private readonly DialogueBox _box;
    private readonly OnBoarding _onBoarding;
    private readonly IUpdater _updater;
    private readonly List<IPanel> _panels;
    private readonly DispatcherTimer _timer;
    private PanelTab _activePanel;
    private bool _foldedMenu;

    public bool FoldedMenu
    {
        get => _foldedMenu;
        set => this.RaiseAndSetIfChanged(ref _foldedMenu, value);
    }
        
    public ReactiveCommand<Unit, Unit> ToggleFoldedCommand { get; }
        
    public PanelTab ActivePanel
    {
        get => _activePanel;
        set => this.RaiseAndSetIfChanged(ref _activePanel, value);
    }

    public ObservableCollection<PanelTab> TopPanels { get; } = [];
    public ObservableCollection<PanelTab> BottomPanels { get; } = [];

    public SteamWidget SteamWidget { get; }
        
    public DialogueBox DialogueBox { get; }

    public async Task OnWindowShow()
    {
        await _steam.Connect();
    }
        
    internal void OnAppClose()
    {
        _launcher.Dispose();
        _steam.Disconnect();
        _timer.Stop();
        Task.Run(() =>
        {
            while (_steam.IsConnected)
                Task.Delay(100);
        }).Wait();
    }

    private async Task OnTabClicked(object? sender, PanelTab tab)
    {
        ActivePanel.Active = false;
        ActivePanel = tab;
        ActivePanel.Active = true;
        if (ActivePanel.Panel is IDisplablePanel displayable)
            await displayable.DisplayPanel();
    }
    
    private async void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            _timer.Stop();
            await _launcher.Tick();
            foreach (var panel in _panels)
                if(panel is ITickingPanel ticking)
                    await ticking.TickPanel();
            _timer.Start();
        }
        catch (OperationCanceledException) {}
        catch (Exception ex)
        {
            await CrashHandler.Handle(ex);
        }
    }

    private async void Start()
    {
        try
        {
            _timer.Start();
            _activePanel.Active = true;
            if(_activePanel.Panel is IDisplablePanel displayed) 
                await displayed.DisplayPanel(); 
            await RefreshPanels();
            await OnBoardingActions();
        }
        catch (OperationCanceledException){}
        catch (Exception ex)
        {
            await CrashHandler.Handle(ex);
        }
        
    }

    private async Task RefreshPanels()
    {
        foreach (var panel in _panels)
            if(panel is IRefreshablePanel refreshable)
                await refreshable.RefreshPanel();
    }

    private async Task OnBoardingActions()
    {
        try
        {
            _appFiles.SetupFolders();
            if (!await _onBoarding.OnBoardingLanguageChoice()) return;
            if (!await _onBoarding.OnBoardingCheckTrebuchet()) return;
            if (!await OnBoardingFirstLaunch()) return;
            if (!await OnBoardingRepairBrokenJunctions()) return;
            if (!await _onBoarding.OnBoardingCheckForUpdate(_updater)) return;
        }
        catch (OperationCanceledException ex)
        {
            await _box.OpenErrorAndExitAsync(ex.Message);
        }
    }

    private async Task<bool> OnBoardingRepairBrokenJunctions()
    {
        var clientDirectory = Path.GetFullPath(_setup.Config.ClientPath);
        if (!Tools.IsClientInstallValid(clientDirectory)) return true;
        return await _onBoarding.OnBoardingApplyConanManagement();
    }

    private async Task<bool> OnBoardingFirstLaunch()
    {
        var configPath = Constants.GetConfigPath(_setup.IsTestLive);
        if(File.Exists(configPath)) return true;
        if (!await _onBoarding.OnBoardingUsageChoice()) return false;
        _setup.Config.SaveFile();
        await RefreshPanels();
        return true;
    }
}