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
        protected Window? _windows;

        public abstract int ModalHeight { get; }
        public abstract string ModalTitle { get; }
        public abstract DataTemplate Template { get; }
        public abstract int ModalWidth { get; }

        public abstract void OnWindowClose();

        public void ShowDialog()
        {
            ModalWindow modal = new ModalWindow(this);
            _windows = modal;
            modal.Height = ModalHeight;
            modal.Width = ModalWidth;
            if (Application.Current.MainWindow.ShowActivated)
                modal.Owner = Application.Current.MainWindow;
            modal.ShowDialog();
        }
    }
}