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
using Trebuchet.Services;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;
using Panel = Trebuchet.ViewModels.Panels.Panel;

namespace Trebuchet.ViewModels;

public sealed class TrebuchetApp : ReactiveObject
{
    public static string VersionHeader => tot_lib.Utils.GetFileVersion();
    
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
        IEnumerable<Panel> panels)
    {
        _setup = setup;
        _appFiles = appFiles;
        _launcher = launcher;
        _steam = steam;
        _box = box;
        _onBoarding = onBoarding;
        _panels = panels.ToList();
        SteamWidget = steamWidget;
        DialogueBox = dialogueBox;
        
        foreach (var panel in _panels)
        {
            panel.PanelSelected += (_, p) => ActivePanel = p;
            panel.RequestAppRefresh += (_,_) => RefreshPanels();
            
            if(panel.BottomPosition)
                BottomPanels.Add(panel);
            else
                TopPanels.Add(panel);
        }

        _activePanel = BottomPanels.First(x => x.CanTabBeClicked);
        this.WhenAnyValue(x => x.ActivePanel)
            .StartWith(_activePanel)
            .Buffer(2, 1)
            .Select(b => (Previous: b[0], Current: b[1]))
            .InvokeCommand(ReactiveCommand.CreateFromTask<(Panel, Panel)>(OnPanelActivated));

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
    private readonly List<Panel> _panels;
    private readonly DispatcherTimer _timer;
    private Panel _activePanel;
    private bool _foldedMenu;

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            _timer.Stop();
            await _launcher.Tick();
            foreach (var panel in _panels)
                await panel.Tick();
            _timer.Start();
        }
        catch (OperationCanceledException) {}
        catch (Exception ex)
        {
            await CrashHandler.Handle(ex);
        }
    }

    public bool FoldedMenu
    {
        get => _foldedMenu;
        set => this.RaiseAndSetIfChanged(ref _foldedMenu, value);
    }
        
    public ReactiveCommand<Unit, Unit> ToggleFoldedCommand { get; }
        
    public Panel ActivePanel
    {
        get => _activePanel;
        set => this.RaiseAndSetIfChanged(ref _activePanel, value);
    }

    public ObservableCollection<Panel> TopPanels { get; } = [];
    public ObservableCollection<Panel> BottomPanels { get; } = [];

    public SteamWidget SteamWidget { get; }
        
    public DialogueBox DialogueBox { get; }

    public async Task OnWindowShow()
    {
        _panels.ForEach(x => x.OnWindowShow());
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

    private async void Start()
    {
        try
        {
            _timer.Start();
            foreach (var panel in _panels)
                await panel.RefreshPanel();
            await OnBoardingActions();
        }
        catch (OperationCanceledException){}
        catch (Exception ex)
        {
            await CrashHandler.Handle(ex);
        }
        
    }

    private async Task OnPanelActivated((Panel previous, Panel current) args)
    {
        args.previous.Active = false;
        args.current.Active = true;
        await args.current.DisplayPanel();
    }

    private async Task RefreshPanels()
    {
        foreach (var panel in _panels)
            await panel.RefreshPanel();
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
            
            _activePanel.Active = true;
            await ActivePanel.DisplayPanel();
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