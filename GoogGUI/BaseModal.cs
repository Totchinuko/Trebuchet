using System.Windows;

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
            if (Application.Current.MainWindow.ShowActivated)
                _window.Owner = Application.Current.MainWindow;
        }

        public abstract int ModalHeight { get; }

        public abstract string ModalTitle { get; }

        public abstract int ModalWidth { get; }

        public abstract DataTemplate Template { get; }

        public void Close() => _window.Close();

        public abstract void OnWindowClose();

        public void Show() => _window.PopDialog(false);

        public void ShowDialog() => _window.PopDialog();
    }
}