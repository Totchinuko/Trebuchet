using System;
using ReactiveUI;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionWait))]
public class SequenceActionWaitViewModel : SequenceActionViewModel<SequenceActionWaitViewModel, SequenceActionWait>
{
    public SequenceActionWaitViewModel(SequenceActionWait action) : base(action)
    {
        _waitTime = action.WaitTime;
    }

    private TimeSpan _waitTime;

    public TimeSpan WaitTime
    {
        get => _waitTime;
        set => this.RaiseAndSetIfChanged(ref _waitTime, value);
    }

    protected override void OnActionChanged()
    {
        Action.WaitTime = WaitTime;
        
        base.OnActionChanged();
    }
}