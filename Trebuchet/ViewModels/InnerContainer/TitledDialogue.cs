using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class TitledDialogue<T> : DialogueContent where T : TitledDialogue<T>
{
    private bool _canCancel;

    public TitledDialogue(string title, string description)
    {
        Title = title;
        Description = description;
        CancelCommand = ReactiveCommand.Create(Close, this.WhenAnyValue(x => x.CanCancel));
    }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; protected set; }
    public string Title { get; }
    public string Description { get; }

    public bool CanCancel
    {
        get => _canCancel;
        protected set => this.RaiseAndSetIfChanged(ref _canCancel, value);
    }

    public T ToggleCancellable(bool toggle)
    {
        CanCancel = toggle;
        return (T)this;
    }
}