using System;
using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels;

public class MixedConsolePopedOutViewModel
{
    public MixedConsolePopedOutViewModel()
    {
        Close = ReactiveCommand.Create(() => Closed?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? Closed;
    
    public ReactiveCommand<Unit,Unit> Close { get; }
}