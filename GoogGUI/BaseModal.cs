using System.Windows;
using System.Windows.Controls;

namespace GoogGUI
{
    public abstract class BaseModal
    {
        protected ModalWindow _window;

        public BaseModal()
        {
            _window = new ModalWindow(this);
            _window.Height = ModalHeight;
            _window.Width = ModalWidth;
            if (((MainWindow)Application.Current.MainWindow).WasShown)
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

        public void Close() => _window.Close();

        public abstract void OnWindowClose();

        public void Show() => _window.PopDialog(false);

        public void ShowDialog() => _window.PopDialog();

        public virtual void Submit()
        {
        }
    }
}