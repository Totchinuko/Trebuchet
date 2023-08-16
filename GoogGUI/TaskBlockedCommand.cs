using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogGUI
{
    public class TaskBlockedCommand : ICommand
    {
        private string[] _tasks = new string[0];
        private Action<object?> _command;
        private bool _enabled;

        public event EventHandler? CanExecuteChanged;

        public TaskBlockedCommand(Action<object?> command, bool enabled = true, params string[] tasks)
        {
            _command = command;
            _tasks = tasks;
            _enabled = enabled;
            App.TaskBlocker.TaskSourceChanged += OnTaskSourceChanged;
        }

        private void OnTaskSourceChanged(object? sender, string e)
        {
            if (_tasks.Contains(e))
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Toggle(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter)
        {
            return !App.TaskBlocker.IsSet(_tasks) && _enabled;
        }

        public void Execute(object? parameter)
        {
            if(CanExecute(parameter))
                _command.Invoke(parameter);
        }
    }
}
