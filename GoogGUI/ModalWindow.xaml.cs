using System;
using System.ComponentModel;
using System.Windows;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for ModalWindow.xaml
    /// </summary>
    public partial class ModalWindow : Window, INotifyPropertyChanged
    {
        private BaseModal _app;

        public ModalWindow(BaseModal modal)
        {
            InitializeComponent();
            _app = modal;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BaseModal App { get => _app; private set => _app = value; }

        public void PopDialog(bool wait = true)
        {
            DataContext = this;
            if (wait)
                ShowDialog();
            else
                Show();
        }

        protected override void OnClosed(EventArgs e)
        {
            _app.OnWindowClose();
            base.OnClosed(e);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}