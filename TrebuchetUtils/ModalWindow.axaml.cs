#region

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

#endregion

namespace TrebuchetUtils
{
    /// <summary>
    ///     Interaction logic for ModalWindow.xaml
    /// </summary>
    public partial class ModalWindow : Window, IShownWindow
    {
        public ModalWindow()
        {
            InitializeComponent();
        }

        public BaseModal? App { get; private set; }

        public string AppIconPath => Application.Current is IApplication app ? app.AppIconPath : string.Empty;

        public bool WasShown { get; private set; }

        public void SetModal(BaseModal modal)
        {
            App = modal;
            DataContext = this;
        }

        public event EventHandler? WindowClosed;

        public void OpenDialogue(Window? window = null)
        {
            if (window != null)
                ShowDialog(window);
            else
                Show();
        }

        protected override void OnClosed(EventArgs e)
        {
            WindowClosed?.Invoke(this, e);
            base.OnClosed(e);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            OnShown();
        }

        protected virtual void OnShown()
        {
            if (WasShown) return;
            WasShown = true;
            var children = GuiExtensions.FindVisualChildren<TextBox>(WindowContent).Where(x =>
            {
                if (x.Tag is string value)
                    return value == "Submit";
                return false;
            }).ToList();

            if (children.Count > 0)
                children.ForEach(x => x.KeyDown += OnPreviewKeyDown);
            else
                KeyDown += OnPreviewKeyDown;
        }

        protected virtual void OnSubmit(object? sender)
        {
            App?.Submit();
        }

        protected virtual void OnCancel(object? sender)
        {
            App?.Cancel();
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    OnSubmit(sender);
                    break;
                case Key.Escape:
                    OnCancel(sender);
                    break;
            }
        }
    }
}