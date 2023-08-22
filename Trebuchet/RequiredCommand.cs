using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Linq;
using System.Windows.Input;

namespace Trebuchet
{
    public class RequiredCommand : ICommand, IRecipient<OperationStateChanged>
    {
        private string _button;
        private Action<object?> _callback;
        private bool _launched;
        private string _message;
        private Operations[] _tasks;

        public RequiredCommand(string message, string button, Action<object?> callback, params Operations[] tasks)
        {
            _tasks = tasks;
            _callback = callback;
            _message = message;
            _button = button;
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        public event EventHandler? CanExecuteChanged;

        public string Button { get => _button; set => _button = value; }

        public bool Launched { get => _launched; private set => _launched = value; }

        public string Message { get => _message; set => _message = value; }

        public bool CanExecute(object? parameter)
        {
            return !StrongReferenceMessenger.Default.Send(new OperationStateRequest(_tasks)) && !_launched;
        }

        public void Execute(object? parameter)
        {
            if (_launched) return;
            _launched = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            _callback.Invoke(parameter);
        }

        void IRecipient<OperationStateChanged>.Receive(OperationStateChanged message)
        {
            if (_tasks.Contains(message.key))
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}