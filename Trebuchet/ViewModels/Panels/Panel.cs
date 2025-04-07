using System;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;

namespace Trebuchet.ViewModels.Panels;

public abstract class Panel : MenuElement
{
    private bool _canTabBeClicked = true;
    private bool _active;
    private string _tabClass = "AppTabNeutral";

    public Panel(string label, string iconPath, bool bottom) : base(label)
    {
        IconPath = iconPath;
        BottomPosition = bottom;
        DisplayPanel = ReactiveCommand.Create(() => {});
        RequestAppRefresh = ReactiveCommand.Create(() => {});
        RefreshPanel = ReactiveCommand.Create(() => { });
        TabClick = ReactiveCommand.Create<Panel,Panel>((p) => p, this.WhenAnyValue(x => x.CanTabBeClicked));

        this.WhenValueChanged(x => x.Active)
            .Subscribe((v) => TabClass = v ? "AppTabBlue" : "AppTabNeutral");
    }

    public string IconPath { get; }
    public bool BottomPosition { get; }
    public ReactiveCommand<Unit, Unit> RequestAppRefresh { get; }
    public ReactiveCommand<Unit, Unit> RefreshPanel { get; }
    public ReactiveCommand<Unit, Unit> DisplayPanel { get; }
    public ReactiveCommand<Panel,Panel> TabClick { get; }

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

    public virtual Task Tick()
    {
        return Task.CompletedTask;
    }
}