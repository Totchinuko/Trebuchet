using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public abstract class BaseModal
    {
        protected Window _window;

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
        public abstract DataTemplate Template { get; }
        public abstract int ModalWidth { get; }

        public abstract void OnWindowClose();

        public void ShowDialog() => _window.ShowDialog();
    }
}