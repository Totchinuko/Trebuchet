﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using tot_lib;
using Trebuchet.Services;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.Panels;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels;

public sealed class TrebuchetApp : ReactiveObject, IDisposable
{
    public static string VersionHeader => ProcessUtil.GetStringVersion();
    
    public TrebuchetApp(
        UIConfig uiConfig,
        SteamWidget steamWidget,
        DialogueBox dialogueBox,
        Operations operations,
        IEnumerable<IPanel> panels)
    {
        _uiConfig = uiConfig;
        _operations = operations;
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
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimerTick);

        Start();
    }
    
    private readonly UIConfig _uiConfig;
    private readonly Operations _operations;
    private readonly List<IPanel> _panels;
    private readonly DispatcherTimer _timer;
    private PanelTab _activePanel;
    private bool _foldedMenu;

    public bool FoldedMenu
    {
        get => _foldedMenu;
        set => this.RaiseAndSetIfChanged(ref _foldedMenu, value);
    }
        
    public PanelTab ActivePanel
    {
        get => _activePanel;
        set => this.RaiseAndSetIfChanged(ref _activePanel, value);
    }

    public ObservableCollection<PanelTab> TopPanels { get; } = [];
    public ObservableCollection<PanelTab> BottomPanels { get; } = [];

    public SteamWidget SteamWidget { get; }
        
    public DialogueBox DialogueBox { get; }

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
            foreach (var panel in _panels)
                if(panel is ITickingPanel ticking)
                    await ticking.TickPanel();
            _timer.Start();
        }
        catch (OperationCanceledException) {}
        catch (Exception ex)
        {
            await App.HandleAppCrash(ex);
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
            await StartActions();
        }
        catch (OperationCanceledException){}
        catch (Exception ex)
        {
            await App.HandleAppCrash(ex);
        }
        
    }

    private async Task RefreshPanels()
    {
        FoldedMenu = _uiConfig.FoldedMenu;
        foreach (var panel in _panels.OfType<IRefreshablePanel>())
            await panel.RefreshPanel();
    }

    private async Task<bool> StartPanelActions()
    {
        foreach (var panel in _panels.OfType<IStartingPanel>())
            if (!await panel.StartPanel())
                return false;
        return true;
    }

    private async Task StartActions()
    {
        try
        {
            if (!await StartPanelActions()) return;
            if (!await _operations.OnBoardingCheckForUpdate()) return;
            if (!await _operations.OnBoardingCheckTrebuchet()) return;
            if (!await _operations.OnBoardingFirstLaunch()) return;
            if (!await _operations.RepairBrokenJunctions()) return;
            await RefreshPanels();
        }
        catch (OperationCanceledException ex)
        {
            await DialogueBox.OpenErrorAndExitAsync(ex.Message);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}