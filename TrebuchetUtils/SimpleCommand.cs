using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TrebuchetUtils
{
    public class SimpleCommand(Action<object?> execute, bool enabled = true) : ICommand
    {
        private bool _enabled = enabled;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _enabled;
        }

        public void Execute(object? parameter)
        {
            if(_enabled)
                execute(parameter);
        }

        public void Toggle(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
