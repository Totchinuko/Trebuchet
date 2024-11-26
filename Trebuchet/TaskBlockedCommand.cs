using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Trebuchet
{
    public class TaskBlockedCommand : ICommand, IRecipient<OperationStateChanged>
    {
        private Action<object?> _command;
        private bool _enabled;
        private Operations[] _tasks;

        public TaskBlockedCommand(Action<object?> command, bool enabled = true, params Operations[] tasks)
        {
            _command = command;
            _tasks = tasks;
            _enabled = enabled;
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !StrongReferenceMessenger.Default.Send(new OperationStateRequest(_tasks)) && _enabled;
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                _command.Invoke(parameter);
        }

        public void Toggle(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        void IRecipient<OperationStateChanged>.Receive(OperationStateChanged message)
        {
            if (_tasks.Contains(message.key))
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}