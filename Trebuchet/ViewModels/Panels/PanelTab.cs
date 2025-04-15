using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using tot_lib;

namespace Trebuchet.ViewModels.Panels;

public sealed class PanelTab : ReactiveObject
{
    public PanelTab(IPanel panel)
    {
        Panel = panel;

        _icon = panel.WhenAnyValue(x => x.Icon)
            .ToProperty(this, x => x.Icon);
        _label = panel.WhenAnyValue(x => x.Label)
            .ToProperty(this, x => x.Label);
        
        Click = ReactiveCommand.CreateFromTask(OnPanelSelected, panel.WhenAnyValue(x => x.CanBeOpened));
    }

    private ObservableAsPropertyHelper<string> _icon;
    private ObservableAsPropertyHelper<string> _label;
    private bool _active;

    public event AsyncEventHandler<PanelTab>? PanelSelected; 
    
    public IPanel Panel { get; }
    public string Icon => _icon.Value;
    public string Label => _label.Value;

    public bool Active
    {
        get => _active;
        set => this.RaiseAndSetIfChanged(ref _active, value);
    }
    public ReactiveCommand<Unit, Unit> Click { get; }
    
    private async Task OnPanelSelected()
    {
        if(PanelSelected is not null)
            await PanelSelected.Invoke(this, this);
    }
}