using ReactiveUI;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionDiscordWebHook))]
public class SequenceActionDiscordWebHookViewModel(SequenceActionDiscordWebHook action) :
    SequenceActionViewModel<SequenceActionDiscordWebHookViewModel, SequenceActionDiscordWebHook>(action)
{
    private string _message = action.Message;
    private string _discordWebHook = action.DiscordWebHook;
    private bool _cancelOnFailure = action.CancelOnFailure;

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public string DiscordWebHook
    {
        get => _discordWebHook;
        set => this.RaiseAndSetIfChanged(ref _discordWebHook, value);
    }

    public bool CancelOnFailure
    {
        get => _cancelOnFailure;
        set => this.RaiseAndSetIfChanged(ref _cancelOnFailure, value);
    }

    protected override void OnActionChanged()
    {
        Action.Message = Message;
        Action.DiscordWebHook = DiscordWebHook;
        Action.CancelOnFailure = CancelOnFailure;
        
        base.OnActionChanged();
    }
}