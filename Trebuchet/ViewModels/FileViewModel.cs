using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using tot_lib;

namespace Trebuchet.ViewModels;

public class FileViewModel : ReactiveObject
{
    public FileViewModel(string name, bool selected, bool exportable)
    {
        Name = name;
        Selected = selected;
        Exportable = exportable;

        Select = ReactiveCommand.CreateFromTask(() => OnClicked(FileViewAction.Select));
        OpenFolder = ReactiveCommand.CreateFromTask(() => OnClicked(FileViewAction.OpenFolder));
        Rename = ReactiveCommand.CreateFromTask(() => OnClicked(FileViewAction.Rename));
        Duplicate = ReactiveCommand.CreateFromTask(() => OnClicked(FileViewAction.Duplicate));
        Export = ReactiveCommand.CreateFromTask(() => OnClicked(FileViewAction.Export));
        Delete = ReactiveCommand.CreateFromTask(() => OnClicked(FileViewAction.Delete));
        TogglePopup = ReactiveCommand.Create(() =>
        {
            IsPopupOpen = !IsPopupOpen;
        });

        _displayButton = this.WhenAnyValue(x => x.IsPopupOpen, x => x.IsOver, (p, o) => p || o)
            .ToProperty(this, x => x.DisplayButton);
    }
    private bool _isPopupOpen = false;
    private bool _isOver = false;
    private bool _selected = false;
    private ObservableAsPropertyHelper<bool> _displayButton;

    public ReactiveCommand<Unit, Unit> Select { get; }
    public ReactiveCommand<Unit, Unit> OpenFolder { get; }
    public ReactiveCommand<Unit, Unit> Rename { get; }
    public ReactiveCommand<Unit, Unit> Duplicate { get; }
    public ReactiveCommand<Unit, Unit> Export { get; }
    public ReactiveCommand<Unit, Unit> Delete { get; }
    public ReactiveCommand<Unit,Unit> TogglePopup { get; }

    public string Name { get; }

    public bool Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }
    public bool Exportable { get; }
    
    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _isPopupOpen, value);
    }

    public bool IsOver
    {
        get => _isOver;
        set => this.RaiseAndSetIfChanged(ref _isOver, value);
    }

    public bool DisplayButton => _displayButton.Value;

    public event AsyncEventHandler<FileViewModelEventArgs>? Clicked;

    protected virtual async Task OnClicked(FileViewAction action)
    {
        IsPopupOpen = false;
        if(Clicked is not null)
            await Clicked.Invoke(this, new FileViewModelEventArgs(Name, action));
    }
}