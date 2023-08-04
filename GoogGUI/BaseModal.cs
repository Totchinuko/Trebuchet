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
        protected Window _windows;

        public BaseModal()
        {
            _windows = new ModalWindow(this);
            _windows.Height = ModalHeight;
            _windows.Width = ModalWidth;
            if (Application.Current.MainWindow.ShowActivated)
                _windows.Owner = Application.Current.MainWindow;
        }

        public abstract int ModalHeight { get; }
        public abstract string ModalTitle { get; }
        public abstract DataTemplate Template { get; }
        public abstract int ModalWidth { get; }

        public abstract void OnWindowClose();

        public void ShowDialog() => _windows.ShowDialog();
    }
}