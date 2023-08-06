using System;
using System.Windows.Input;

namespace GoogGUI
{
    public class RequiredAction : ICommand
    {
        private string _button;
        private Action<object?> _callback;
        private string _message;

        public RequiredAction(string message, string button, Action<object?> callback)
        {
            _callback = callback;
            _message = message;
            _button = button;
        }

        public event EventHandler? CanExecuteChanged;

        public string Button { get => _button; set => _button = value; }

        public string Message { get => _message; set => _message = value; }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _callback.Invoke(parameter);
        }
    }
}