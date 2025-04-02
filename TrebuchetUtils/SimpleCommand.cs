using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia.Utilities;

namespace TrebuchetUtils
{
    public class SimpleCommand() : ICommand
    {
        private bool _enabled = true;
        private event EventHandler<object?>? Executed;
        public event EventHandler? CanExecuteChanged;

        public virtual bool CanExecute(object? parameter)
        {
            return _enabled;
        }

        public void Execute(object? parameter)
        {
            if(CanExecute(parameter))
                OnExecuted(parameter);
        }

        public SimpleCommand Subscribe(EventHandler<object?> action)
        {
            Executed += action;
            return this;
        }

        public SimpleCommand Subscribe(Action<object?> action)
        {
            Executed += (_,parameter) => action(parameter);
            return this;
        }
        
        public SimpleCommand Subscribe(Action action)
        {
            Executed += (_,_) => action();
            return this;
        }
        
        public SimpleCommand Clear()
        {
            Executed = null;
            return this;
        }

        public SimpleCommand Unsubscribe(EventHandler<object?> action)
        {
            Executed -= action;
            return this;
        }

        public SimpleCommand Toggle(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return this;
        }

        protected virtual void OnExecuted(object? parameter)
        {
            Executed?.Invoke(this, parameter);
        }

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
