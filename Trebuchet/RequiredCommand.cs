using System;
using System.Collections.Generic;
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
        private bool _blocked;
        private readonly List<Type> _types = [];

        public RequiredCommand(string message, string button, Action<object?> callback)
        {
            _callback = callback;
            Message = message;
            Button = button;
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        public event EventHandler? CanExecuteChanged;

        public string Button { get; set; }

        public string Message { get; set; }

        public RequiredCommand SetBlockingType<T>() where T : IBlockedTaskType
        {
            _types.Add(typeof(T));
            return this;
        }
        
        public bool CanExecute(object? parameter)
        {
            return !_blocked && !_launched;
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
            if (_types.Contains(message.Type.GetType()))
            {
                _blocked = message.Value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}