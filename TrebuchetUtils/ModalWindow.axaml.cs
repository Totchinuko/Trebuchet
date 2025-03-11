using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;

namespace TrebuchetUtils
{
    /// <summary>
    /// Interaction logic for ModalWindow.xaml
    /// </summary>
    public partial class ModalWindow : Window, IShownWindow
    {
        private BaseModal _app;
        private bool _shown;
        private Window? _ownerWindow;

        public ModalWindow(BaseModal modal, Window? owner)
        {
            _app = modal;
            _ownerWindow = owner;
            DataContext = this;
            InitializeComponent();
        }

        public BaseModal App { get => _app; private set => _app = value; }

        //TODO: User an interface to get the icon from the main app
        public string AppIconPath => (Application.Current as TrebuchetBaseApp)?.AppIconPath ?? string.Empty;

        public void PopDialog()
        {
            if (_ownerWindow != null)
                ShowDialog(_ownerWindow);
            else
                Show();
        }

        public bool WasShown()
        {
            return _shown;
        }

        public void SetOwner(Window? owner)
        {
            this.Owner = owner;
        }

        protected override void OnClosed(EventArgs e)
        {
            _app.OnWindowClose();
            base.OnClosed(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown) return;
            _shown = true;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            OnShown();
        }

        protected virtual void OnShown()
        {
            var childs = GuiExtensions.FindVisualChildren<TextBox>((DependencyObject)WindowContent).Where((x) =>
            {
                if (x.Tag is string value)
                    return value == "Submit";
                return false;
            }).ToList();

            if (childs.Count > 0)
                childs.ForEach(x => x.KeyDown += OnPreviewKeyDown);
            else
                KeyDown += OnPreviewKeyDown;
        }

        protected virtual void OnSubmit(object sender)
        {
            _app.Submit();
        }

        protected virtual void OnCancel(object sender)
        {
            _app.Cancel();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnSubmit(sender);
            else if(e.Key == Key.Escape)
                OnCancel(sender);
        }
    }
}