using System;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionBackupServerData))]
public class SequenceActionBackupServerDataViewModel : 
    SequenceActionViewModel<SequenceActionBackupServerDataViewModel,SequenceActionBackupServerData>
{
    public SequenceActionBackupServerDataViewModel(SequenceActionBackupServerData action) : base(action)
    {
        MaxAge = action.MaxAge;
        CancelOnFailure = action.CancelOnFailure;
    }
    private TimeSpan _maxAge;
    private bool _cancelOnFailure;

    public TimeSpan MaxAge
    {
        get => _maxAge;
        set => this.RaiseAndSetIfChanged(ref _maxAge, value);
    }

    public bool CancelOnFailure
    {
        get => _cancelOnFailure;
        set => this.RaiseAndSetIfChanged(ref _cancelOnFailure, value);
    }

    protected override void OnActionChanged()
    {
        Action.CancelOnFailure = CancelOnFailure;
        Action.MaxAge = MaxAge;
        
        base.OnActionChanged();
    }
}