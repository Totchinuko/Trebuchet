using System;
using System.Collections.Generic;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.Services.TaskBlocker
{
    public class TaskBlockedCommand : SimpleCommand, ITinyRecipient<BlockedTaskStateChanged>
    {
        private bool _blocked;
        private readonly List<Type> _types = [];

        public TaskBlockedCommand SetBlockingType<T>() where T : IBlockedTaskType
        {
            _types.Add(typeof(T));
            return this;
        }

        public override bool CanExecute(object? parameter)
        {
            return !_blocked && base.CanExecute(parameter);
        }

        void ITinyRecipient<BlockedTaskStateChanged>.Receive(BlockedTaskStateChanged message)
        {
            if (_types.Contains(message.Type.GetType()))
            {
                _blocked = message.Value;
                OnCanExecuteChanged();
            }
        }
    }
}