using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using Trebuchet.Services.TaskBlocker;

namespace Trebuchet
{
    public class TaskBlockedCommand : ICommand, IRecipient<BlockedTaskStateChanged>
    {
        private readonly Action<object?> _command;
        private bool _enabled;
        private bool _blocked;
        private readonly List<Type> _types = [];

        public TaskBlockedCommand(Action<object?> command, bool enabled = true)
        {
            _command = command;
            _enabled = enabled;
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        public event EventHandler? CanExecuteChanged;

        public TaskBlockedCommand SetBlockingType<T>() where T : IBlockedTaskType
        {
            _types.Add(typeof(T));
            return this;
        }

        public bool CanExecute(object? parameter)
        {
            return !_blocked && _enabled;
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