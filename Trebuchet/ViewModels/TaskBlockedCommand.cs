using System;
using System.Collections.Generic;
using System.Windows.Input;
using Trebuchet.Services.TaskBlocker;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class TaskBlockedCommand : ICommand, ITinyRecipient<BlockedTaskStateChanged>
    {
        private readonly Action<object?> _command;
        private bool _enabled;
        private bool _blocked;
        private readonly List<Type> _types = [];

        public TaskBlockedCommand(Action<object?> command, bool enabled = true)
        {
            _command = command;
            _enabled = enabled;
            TinyMessengerHub.Default.Subscribe(this);
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

        void ITinyRecipient<BlockedTaskStateChanged>.Receive(BlockedTaskStateChanged message)
        {
            if (_types.Contains(message.Type.GetType()))
            {
                _blocked = message.Value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}