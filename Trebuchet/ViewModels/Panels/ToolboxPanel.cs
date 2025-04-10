using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.ViewModels.InnerContainer;

namespace Trebuchet.ViewModels.Panels;

public class ToolboxPanel : Panel
{

    public ToolboxPanel(
        OnBoarding onBoarding,
        SteamAPI steamApi, 
        DialogueBox box
        ) : base(Resources.PanelToolbox, "mdi-toolbox", true)
    {
        _onBoarding = onBoarding;
        _steamApi = steamApi;
        _box = box;

        _unusedModsSub = this.WhenAnyValue(x => x.UnusedMods)
            .Select(x => string.Format(Resources.TrimUnusedModsSub, x))
            .ToProperty(this, x => x.UnusedModsSub);
        
        RemoveUnusedMods = ReactiveCommand.CreateFromTask(OnRemoveUnusedMods, this.WhenAnyValue(x => x.UnusedMods, x => x > 0));
        DisplayPanel.Subscribe((_) =>
        {
            UnusedMods = _steamApi.CountUnusedMods();
        });
    }
    
    private readonly OnBoarding _onBoarding;
    private readonly SteamAPI _steamApi;
    private readonly DialogueBox _box;
    private int _unusedMods;
    private ObservableAsPropertyHelper<string> _unusedModsSub;

    public string UnusedModsSub => _unusedModsSub.Value;
    public int UnusedMods
    {
        get => _unusedMods;
        set => this.RaiseAndSetIfChanged(ref _unusedMods, value);
    }

    public ReactiveCommand<Unit,Unit> RemoveUnusedMods { get; }
    
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