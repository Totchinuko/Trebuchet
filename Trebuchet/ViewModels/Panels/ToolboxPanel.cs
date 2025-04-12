using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services;

namespace Trebuchet.ViewModels.Panels;

public class ToolboxPanel : Panel
{

    public ToolboxPanel(
        OnBoarding onBoarding,
        SteamApi steamApi 
        ) : base(Resources.PanelToolbox, "mdi-toolbox", true)
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

    public string UnusedModsSub => _unusedModsSub.Value;
    public int UnusedMods
    {
        get => _unusedMods;
        set => this.RaiseAndSetIfChanged(ref _unusedMods, value);
    }

    public ReactiveCommand<Unit,Unit> RemoveUnusedMods { get; }

    public override Task DisplayPanel()
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