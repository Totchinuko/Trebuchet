using System;
using ReactiveUI;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionWaitOnline))]
public class SequenceActionWaitOnlineViewModel(SequenceActionWaitOnline action) : 
    SequenceActionViewModel<SequenceActionWaitOnlineViewModel, SequenceActionWaitOnline>(action)
{
    private bool _cancelOnFailure = action.CancelOnFailure;
    private TimeSpan _timeOut = action.TimeOut;

    public bool CancelOnFailure
    {
        get => _cancelOnFailure;
        set => this.RaiseAndSetIfChanged(ref _cancelOnFailure, value);
    }

    public TimeSpan TimeOut
    {
        get => _timeOut;
        set => this.RaiseAndSetIfChanged(ref _timeOut, value);
    }
    
    protected override void OnActionChanged()
    {
        Action.CancelOnFailure = CancelOnFailure;
        Action.TimeOut = TimeOut;
        
        base.OnActionChanged();
    }
}