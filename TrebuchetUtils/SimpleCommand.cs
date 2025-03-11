using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TrebuchetUtils
{
    public class SimpleCommand : ICommand
    {
        private bool _enabled = true;

        public event EventHandler? CanExecuteChanged;

        private readonly Action<object?> _execute;

        public SimpleCommand(Action<object?> execute, bool enabled = true)
        {
            _execute = execute;
            _enabled = enabled;
        }

        public bool CanExecute(object? parameter)
        {
            return _enabled;
        }

        public void Execute(object? parameter)
        {
            if(_enabled)
                _execute(parameter);
        }

        public void Toggle(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
