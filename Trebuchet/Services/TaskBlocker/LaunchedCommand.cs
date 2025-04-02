using System;
using System.Collections.Generic;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.Services.TaskBlocker
{
    public class LaunchedCommand : SimpleCommand, ITinyRecipient<BlockedTaskStateChanged>
    {
        private bool _launched;
        private bool _blocked;
        private readonly List<Type> _types = [];

        public LaunchedCommand SetBlockingType<T>() where T : IBlockedTaskType
        {
            _types.Add(typeof(T));
            return this;
        }

        public void ResetLaunch()
        {
            _launched = false;
            OnCanExecuteChanged();
        }
        
        public override bool CanExecute(object? parameter)
        {
            return !_blocked && !_launched && base.CanExecute(parameter);
        }

        protected override void OnExecuted(object? parameter)
        {
            _launched = true;
            OnCanExecuteChanged();
            base.OnExecuted(parameter);
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