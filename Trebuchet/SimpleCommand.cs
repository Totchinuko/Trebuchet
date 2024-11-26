using System;
using System.Windows.Input;

namespace Trebuchet
{
    public class SimpleCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private bool _enabled = true;

        public SimpleCommand(Action<object?> execute, bool enabled = true)
        {
            _execute = execute;
            _enabled = enabled;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _enabled;
        }

        public void Execute(object? parameter)
        {
            if (_enabled)
                _execute(parameter);
        }

        public void Toggle(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}