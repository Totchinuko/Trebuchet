using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class TitledDialogue<T> : DialogueContent where T : TitledDialogue<T>
{
    public TitledDialogue(string title, string description)
    {
        Title = title;
        Description = description;
        CancelCommand = ReactiveCommand.Create(Close);
    }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; protected set; }
    public string Title { get; }
    public string Description { get; }
}