﻿using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using Trebuchet.Services.TaskBlocker;

namespace Trebuchet
{
    public class RequiredCommand : ICommand, IRecipient<BlockedTaskStateChanged>
    {
        private readonly Action<object?> _callback;
        private bool _launched;
        private readonly Operations[] _tasks;

        public RequiredCommand(string message, string button, Action<object?> callback, params Operations[] tasks)
        {
            _tasks = tasks;
            _callback = callback;
            Message = message;
            Button = button;
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        public event EventHandler? CanExecuteChanged;

        public string Button { get; set; }

        public string Message { get; set; }

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

        void IRecipient<BlockedTaskStateChanged>.Receive(BlockedTaskStateChanged message)
        {
            if (_tasks.Contains(message.key))
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}