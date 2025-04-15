using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services;

namespace Trebuchet.ViewModels.Panels;

public class ToolboxPanel : ReactiveObject, IDisplablePanel
{

    public ToolboxPanel(
        OnBoarding onBoarding,
        SteamApi steamApi 
        )
    {
        _onBoarding = onBoarding;
        _steamApi = steamApi;

        _unusedModsSub = this.WhenAnyValue(x => x.UnusedMods)
            .Select(x => string.Format(Resources.TrimUnusedModsSub, x))
            .ToProperty(this, x => x.UnusedModsSub);
        
        RemoveUnusedMods = ReactiveCommand.CreateFromTask(OnRemoveUnusedMods, this.WhenAnyValue(x => x.UnusedMods, x => x > 0));
    }
    
    private readonly OnBoarding _onBoarding;
    private readonly SteamApi _steamApi;
    private int _unusedMods;
    private readonly ObservableAsPropertyHelper<string> _unusedModsSub;
    private bool _canBeOpened = true;

    public string UnusedModsSub => _unusedModsSub.Value;
    public int UnusedMods
    {
        get => _unusedMods;
        set => this.RaiseAndSetIfChanged(ref _unusedMods, value);
    }

    public string Icon => @"mdi-toolbox";
    public string Label => Resources.PanelToolbox;

    public bool CanBeOpened
    {
        get => _canBeOpened;
        set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
    }

    public ReactiveCommand<Unit,Unit> RemoveUnusedMods { get; }

    public Task DisplayPanel()
    {
        UnusedMods = _steamApi.CountUnusedMods();
        return Task.CompletedTask;
    }

    private async Task OnRemoveUnusedMods()
    {
        try
        {
            await _onBoarding.OnBoardingRemoveUnusedMods();
            UnusedMods = _steamApi.CountUnusedMods();
        }
        catch(OperationCanceledException) {}
    }
}