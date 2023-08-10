using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogGUI
{
    internal class TaskBlockedCommand : ICommand
    {
        private string _taskKey = string.Empty;
        private Action<object?> _command;

        public event EventHandler? CanExecuteChanged;

        public TaskBlockedCommand(Action<object?> command, string taskKey = TaskBlocker.MainTask)
        {
            _command = command;
            _taskKey = taskKey;
            App.TaskBlocker.TaskSourceChanged += OnTaskSourceChanged;
        }

        private void OnTaskSourceChanged(object? sender, string e)
        {
            if (_taskKey == e)
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter)
        {
            return !App.TaskBlocker.IsSet(_taskKey);
        }

        public void Execute(object? parameter)
        {
            if(CanExecute(parameter))
                _command.Invoke(parameter);
        }
    }
}
