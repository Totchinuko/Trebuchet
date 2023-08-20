using System.Windows;

namespace Trebuchet
{
    public abstract class BaseModal
    {
        protected ModalWindow _window;

        public BaseModal()
        {
            _window = new ModalWindow(this);
            _window.Height = ModalHeight;
            _window.Width = ModalWidth;
            // User should not be able to go back on the main window as long as the modal was not removed..
            // But in some cases we want to be to show it without a main window open
            if (Application.Current.MainWindow is MainWindow && ((MainWindow)Application.Current.MainWindow).WasShown)
            {
                _window.Owner = Application.Current.MainWindow;
                _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
                _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public virtual bool CloseDisabled => true;

        public virtual bool MaximizeDisabled => true;

        public virtual bool MinimizeDisabled => true;

        public abstract int ModalHeight { get; }

        public abstract string ModalTitle { get; }

        public abstract int ModalWidth { get; }

        public abstract DataTemplate Template { get; }

        public Window Window => _window;

        public void Close() => _window.Close();

        public abstract void OnWindowClose();

        public void Show() => _window.PopDialog(false);

        public void ShowDialog() => _window.PopDialog();

        public virtual void Submit()
        {
        }
    }
}