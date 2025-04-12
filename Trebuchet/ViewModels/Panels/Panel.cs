using System;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using tot_lib;

namespace Trebuchet.ViewModels.Panels;

public abstract class Panel : MenuElement
{
    protected Panel(string label, [Localizable(false)] string iconPath, bool bottom) : base(label)
    {
        IconPath = iconPath;
        BottomPosition = bottom;
        TabClick = ReactiveCommand.Create<Panel>(OnPanelSelected, this.WhenAnyValue(x => x.CanTabBeClicked));

        this.WhenValueChanged(x => x.Active)
            .Subscribe((v) => TabClass = v ? @"AppTabBlue" : @"AppTabNeutral");
    }
    private bool _canTabBeClicked = true;
    private bool _active;
    private string _tabClass = @"AppTabNeutral";

    public event AsyncEventHandler? RequestAppRefresh;
    public event EventHandler<Panel>? PanelSelected; 

    public string IconPath { get; }
    public bool BottomPosition { get; }
    public ReactiveCommand<Panel,Unit> TabClick { get; }

    public bool CanTabBeClicked
    {
        get => _canTabBeClicked;
        protected set => this.RaiseAndSetIfChanged(ref _canTabBeClicked, value);
    }

    public bool Active
    {
        get => _active;
        set => this.RaiseAndSetIfChanged(ref _active, value);
    }

    public string TabClass
    {
        get => _tabClass;
        protected set => this.RaiseAndSetIfChanged(ref _tabClass, value);
    }

    public virtual Task Tick() => Task.CompletedTask;
    public virtual Task RefreshPanel() => Task.CompletedTask;
    public virtual Task DisplayPanel() => Task.CompletedTask;

    protected virtual async Task OnRequestAppRefresh()
    {
        if(RequestAppRefresh is not null)
            await RequestAppRefresh.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPanelSelected(Panel args)
    {
        if(PanelSelected is not null)
            PanelSelected.Invoke(this, args);
    }
}