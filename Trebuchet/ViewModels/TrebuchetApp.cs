using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils.Modals;
using Panel = Trebuchet.ViewModels.Panels.Panel;

namespace Trebuchet.ViewModels;

public sealed class TrebuchetApp : ReactiveObject
{
    private readonly AppSetup _setup;
    private readonly AppFiles _appFiles;
    private readonly Launcher _launcher;
    private readonly Steam _steam;
    private readonly DialogueBox _box;
    private readonly OnBoarding _onBoarding;
    private Panel _activePanel;
    private List<Panel> _panels;
    private DispatcherTimer _timer;
    private bool _foldedMenu;

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

        FoldedMenu = uiConfig.FoldedMenu;
        ToggleFoldedCommand = ReactiveCommand.Create(() =>
        {
            FoldedMenu = !FoldedMenu;
            uiConfig.FoldedMenu = FoldedMenu;
            uiConfig.SaveFile();
        });

        InitializePanels(_panels);
        _activePanel = BottomPanels.First(x => x.CanTabBeClicked);
            
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimerTick);
        _timer.Start();
            
        OnBoardingActions();
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        await _launcher.Tick();
        foreach (var panel in _panels)
            await panel.Tick();
        _timer.Start();
    }

    public static string AppTitle => @$"Tot ! Trebuchet {TrebuchetUtils.Utils.GetFileVersion()}";

    public bool FoldedMenu
    {
        get => _foldedMenu;
        set => this.RaiseAndSetIfChanged(ref _foldedMenu, value);
    }
        
    public ReactiveCommand<Unit, Unit> ToggleFoldedCommand { get; }
        
    public Panel ActivePanel
    {
        get => _activePanel;
        set
        {
            if(_activePanel == value) return;
            _activePanel.Active = false;
            this.RaiseAndSetIfChanged(ref _activePanel, value);
            _activePanel.Active = true;
            _activePanel.DisplayPanel.Execute();
        }
    }

    public ObservableCollection<Panel> TopPanels { get; } = [];
    public ObservableCollection<Panel> BottomPanels { get; } = [];

    public SteamWidget SteamWidget { get; }
        
    public DialogueBox DialogueBox { get; }

    public async void OnWindowShow()
    {
        _panels.ForEach(x => x.OnWindowShow());
        await _steam.Connect();
    }
        
    internal void OnAppClose()
    {
        _launcher.Dispose();
        _steam.Disconnect();
        Task.Run(() =>
        {
            while (_steam.IsConnected)
                Task.Delay(100);
        }).Wait();
    }

    private void InitializePanels(List<Panel> panels)
    {
        foreach (var panel in panels)
        {
            panel.TabClick.Subscribe((p) => ActivePanel = p);
            panel.RequestAppRefresh.Subscribe((_) => RefreshPanels());
            panel.RefreshPanel.Execute().Subscribe();
            
            if(panel.BottomPosition)
                BottomPanels.Add(panel);
            else
                TopPanels.Add(panel);
        }
    }

    private void RefreshPanels()
    {
        foreach (var panel in _panels)
            panel.RefreshPanel.Execute().Subscribe();
    }

    private async void OnBoardingActions()
    {
        try
        {
            _appFiles.SetupFolders();
            if (!await _onBoarding.OnBoardingCheckTrebuchet()) return;
            if (!await OnBoardingFirstLaunch()) return;
        }
        catch (Exception ex)
        {
            await _box.OpenErrorAndExitAsync(ex.Message);
            return;
        }
        
        _activePanel.Active = true;
        _activePanel.DisplayPanel.Execute();
    }

    private async Task<bool> OnBoardingFirstLaunch()
    {
        var configPath = AppConstants.GetConfigPath(_setup.IsTestLive);
        if(File.Exists(configPath)) return true;
        if (!await _onBoarding.OnBoardingUsageChoice()) return false;
        _setup.Config.SaveFile();
        RefreshPanels();
        return true;
    }
}