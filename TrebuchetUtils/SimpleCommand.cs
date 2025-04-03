using System;
using System.Windows.Input;

namespace TrebuchetUtils
{
    public class SimpleCommand : SimpleCommand<object?>
    {
    }
    
    public class SimpleCommand<P> : ICommand
    {
        private Func<P?, bool> _canExecute;
        private event EventHandler<P?>? Executed;
        public event EventHandler? CanExecuteChanged;
        
        public SimpleCommand()
        {
            _canExecute = (_) => true;
        }
        
        public SimpleCommand(Func<P?,bool> canExecute)
        {
            _canExecute = canExecute;
        }
        
        public SimpleCommand(EventHandler<P?> execute, Func<P?,bool> canExecute) : this(canExecute)
        {
            Executed += execute;
        }

        public virtual bool CanExecute(object? parameter)
        {
            if (parameter is P canExecuteParam)
                return _canExecute(canExecuteParam);
            if (parameter is null)
                return _canExecute((P?)parameter);
            return false;
        }

        public void Execute(object? parameter)
        {
            
            if(parameter is P executeParameter)
                OnExecuted(executeParameter);
            if(parameter is null)
                OnExecuted((P?)parameter);
        }

        public SimpleCommand<P> Subscribe(EventHandler<P?> action)
        {
            Executed += action;
            return this;
        }

        public SimpleCommand<P> Subscribe(Action<P?> action)
        {
            Executed += (_,parameter) => action(parameter);
            return this;
        }
        
        public SimpleCommand<P> Subscribe(Action action)
        {
            Executed += (_,_) => action();
            return this;
        }
        
        public SimpleCommand<P> Clear()
        {
            Executed = null;
            return this;
        }

        public SimpleCommand<P> Unsubscribe(EventHandler<P?> action)
        {
            Executed -= action;
            return this;
        }

        public SimpleCommand<P> SetCanExecute(Func<P?, bool> canExecute)
        {
            _canExecute = canExecute;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SimpleCommand<P> Toggle(bool enabled)
        {
            if (enabled)
                return SetCanExecute((_) => true);
            return SetCanExecute((_) => false);
        }

        protected virtual void OnExecuted(P? parameter)
        {
            Executed?.Invoke(this, parameter);
        }

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
