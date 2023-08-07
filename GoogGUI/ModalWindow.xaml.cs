using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for ModalWindow.xaml
    /// </summary>
    public partial class ModalWindow : Window, INotifyPropertyChanged
    {
        private BaseModal _app;
        private bool _focused;

        public ModalWindow(BaseModal modal)
        {
            _app = modal;
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BaseModal App { get => _app; private set => _app = value; }

        public void PopDialog(bool wait = true)
        {
            if (wait)
                ShowDialog();
            else
                Show();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            OnShown();
        }

        protected virtual void OnShown()
        {
            if (_focused) return;
            _focused = true;
            TextBox? textbox = GuiExtensions.FindVisualChildren<TextBox>((DependencyObject)WindowContent).Where(x => x.Name == "ToFocus").FirstOrDefault();
            if(textbox != null)
            {
                textbox.Focus();
                textbox.SelectAll();
            }

            var childs = GuiExtensions.FindVisualChildren<TextBox>((DependencyObject)WindowContent).Where((x) => 
            {
                if (x.Tag is string value)
                    return value == "Submit";
                return false;
            }).ToList();

            childs.ForEach(x => x.KeyDown += OnPreviewKeyDown);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnSubmit(sender);
        }

        protected virtual void OnSubmit(object sender)
        {
            _app.Submit();
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