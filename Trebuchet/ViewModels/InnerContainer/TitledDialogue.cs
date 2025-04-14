using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class TitledDialogue<T> : DialogueContent where T : TitledDialogue<T>
{
    protected TitledDialogue()
    {
        CancelCommand = ReactiveCommand.Create(Close);
    }
    
    protected TitledDialogue(string title, string description) : this()
    {
        Title = title;
        Description = description;
    }

    private string _title = string.Empty;
    private string _description = string.Empty;

    public ReactiveCommand<Unit, Unit> CancelCommand { get; protected set; }

    public string Title
    {
        get => _title;
        protected set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    public string Description
    {
        get => _description;
        protected set => this.RaiseAndSetIfChanged(ref _description, value);
    }
}