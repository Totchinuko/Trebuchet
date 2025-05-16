using ReactiveUI;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionExecuteProcess))]
public class SequenceActionExecuteProcessViewModel(SequenceActionExecuteProcess action) : 
    SequenceActionViewModel<SequenceActionExecuteProcessViewModel, SequenceActionExecuteProcess>(action)
{
    private string _filename = action.Filename;
    private string _arguments = action.Arguments;
    private bool _waitForProcessToExit = action.WaitForProcessToExit;
    private bool _cancelIfExitCodeIsError = action.CancelIfExitCodeIsError;
    private bool _createNoWindow = action.CreateNoWindow;
    private bool _useShellExecute = action.UseShellExecute;
    private bool _cancelOnFailure = action.CancelOnFailure;

    public string Filename
    {
        get => _filename;
        set => this.RaiseAndSetIfChanged(ref _filename, value);
    }

    public string Arguments
    {
        get => _arguments;
        set => this.RaiseAndSetIfChanged(ref _arguments, value);
    }

    public bool WaitForProcessToExit
    {
        get => _waitForProcessToExit;
        set => this.RaiseAndSetIfChanged(ref _waitForProcessToExit, value);
    }

    public bool CancelIfExitCodeIsError
    {
        get => _cancelIfExitCodeIsError;
        set => this.RaiseAndSetIfChanged(ref _cancelIfExitCodeIsError, value);
    }

    public bool CreateNoWindow
    {
        get => _createNoWindow;
        set => this.RaiseAndSetIfChanged(ref _createNoWindow, value);
    }

    public bool UseShellExecute
    {
        get => _useShellExecute;
        set => this.RaiseAndSetIfChanged(ref _useShellExecute, value);
    }

    public bool CancelOnFailure
    {
        get => _cancelOnFailure;
        set => this.RaiseAndSetIfChanged(ref _cancelOnFailure, value);
    }

    protected override void OnActionChanged()
    {
        Action.Filename = Filename;
        Action.Arguments = Arguments;
        Action.CancelOnFailure = CancelOnFailure;
        Action.CancelIfExitCodeIsError = CancelIfExitCodeIsError;
        Action.CreateNoWindow = CreateNoWindow;
        Action.UseShellExecute = UseShellExecute;
        Action.WaitForProcessToExit = WaitForProcessToExit;
        
        base.OnActionChanged();
    }
}