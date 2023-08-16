﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace GoogGUI
{
    public class RequiredCommand : ICommand
    {
        private string _button;
        private Action<object?> _callback;
        private bool _launched;
        private string _message;
        private string[] _tasks;

        public RequiredCommand(string message, string button, Action<object?> callback, params string[] tasks)
        {
            _tasks = tasks;
            _callback = callback;
            _message = message;
            _button = button;
            if (_tasks.Length > 0)
                App.TaskBlocker.PropertyChanged += OnTaskBlockerPropertyChanged;
        }

        public event EventHandler? CanExecuteChanged;

        public string Button { get => _button; set => _button = value; }

        public bool Launched { get => _launched; private set => _launched = value; }

        public string Message { get => _message; set => _message = value; }

        public bool CanExecute(object? parameter)
        {
            return !App.TaskBlocker.IsSet(_tasks) && !_launched;
        }

        public void Execute(object? parameter)
        {
            if (_launched) return;
            _launched = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            _callback.Invoke(parameter);
        }

        private void OnTaskBlockerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsAvailable")
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}