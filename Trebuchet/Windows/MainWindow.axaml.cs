using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Trebuchet.ViewModels;
using TrebuchetUtils;

namespace Trebuchet.Windows
{
    public partial class MainWindow : WindowAutoPadding
    {
        private bool _shown;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (_shown) return;
            _shown = true;
            FlyoutBase.ShowAttachedFlyout(MainBorder);
        }
    }
}