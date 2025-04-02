using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class TitledDialogue : DialogueContent
{
    public TitledDialogue(string title, string description)
    {
        Title = title;
        Description = description;
        CancelCommand = new SimpleCommand().Subscribe(Close);
    }
    
    public SimpleCommand CancelCommand { get; }
    public string Title { get; }
    public string Description { get; }

    public TitledDialogue ToggleCancellable(bool toggle)
    {
        CancelCommand.Toggle(toggle);
        return this;
    }
}