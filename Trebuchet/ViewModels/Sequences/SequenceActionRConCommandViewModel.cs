using ReactiveUI;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionRConCommand))]
public class SequenceActionRConCommandViewModel(SequenceActionRConCommand action) : 
    SequenceActionViewModel<SequenceActionRConCommandViewModel, SequenceActionRConCommand>(action)
{
    private string _rConCommand = action.RConCommand;
    private bool _cancelOnFailure = action.CancelOnFailure;

    public string RConCommand
    {
        get => _rConCommand;
        set => this.RaiseAndSetIfChanged(ref _rConCommand, value);
    }

    public bool CancelOnFailure
    {
        get => _cancelOnFailure;
        set => this.RaiseAndSetIfChanged(ref _cancelOnFailure, value);
    }

    protected override void OnActionChanged()
    {
        Action.RConCommand = RConCommand;
        Action.CancelOnFailure = CancelOnFailure;
        
        base.OnActionChanged();
    }
}