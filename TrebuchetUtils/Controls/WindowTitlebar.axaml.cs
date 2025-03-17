using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace TrebuchetUtils.Controls
{
    /// <summary>
    /// Interaction logic for WindowTitlebar.xaml
    /// </summary>
    public partial class WindowTitlebar : UserControl
    {
        public static readonly StyledProperty<IImage?> CloseIconProperty = AvaloniaProperty.Register<WindowTitlebar, IImage?>(nameof(CloseIcon));

        public static readonly StyledProperty<bool> DisableCloseProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(DisableClose));

        public static readonly StyledProperty<bool> DisableMaximizeProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(DisableMaximize));
        
        public static readonly StyledProperty<bool> DisableMinimizeProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(DisableMinimize));

        public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<WindowTitlebar, object?>(nameof(Header));

        public static readonly StyledProperty<IImage?> LogoIconProperty = AvaloniaProperty.Register<WindowTitlebar, IImage?>(nameof(LogoIcon));

        public static readonly StyledProperty<IImage?> MaximizeIconProperty = AvaloniaProperty.Register<WindowTitlebar, IImage?>(nameof(MaximizeIcon));

        public static readonly StyledProperty<IImage?> MinimizeIconProperty = AvaloniaProperty.Register<WindowTitlebar, IImage?>(nameof(MinimizeIcon));

        public static readonly StyledProperty<IImage?> RestoreIconProperty = AvaloniaProperty.Register<WindowTitlebar, IImage?>(nameof(RestoreIcon));

        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<WindowTitlebar, string>(nameof(Title));

        private WindowState _state;

        public WindowTitlebar()
        {
            CloseIconProperty.Changed.AddClassHandler<WindowTitlebar>(OnCloseIconChanged);
            DisableCloseProperty.Changed.AddClassHandler<WindowTitlebar>(OnDisableChanged);
            DisableMaximizeProperty.Changed.AddClassHandler<WindowTitlebar>(OnDisableChanged);
            DisableMinimizeProperty.Changed.AddClassHandler<WindowTitlebar>(OnDisableChanged);
            LogoIconProperty.Changed.AddClassHandler<WindowTitlebar>(OnLogoIconChanged);
            MaximizeIconProperty.Changed.AddClassHandler<WindowTitlebar>(OnMaximizeIconChanged);
            MinimizeIconProperty.Changed.AddClassHandler<WindowTitlebar>(OnMinimizeIconChanged);
            RestoreIconProperty.Changed.AddClassHandler<WindowTitlebar>(OnRestoreIconChanged);
            TitleProperty.Changed.AddClassHandler<WindowTitlebar>(OnTitleChanged);
            InitializeComponent();
        }

        public IImage? CloseIcon
        {
            get => GetValue(CloseIconProperty);
            set => SetValue(CloseIconProperty, value);
        }

        public bool DisableClose
        {
            get => GetValue(DisableCloseProperty);
            set => SetValue(DisableCloseProperty, value);
        }

        public bool DisableMaximize
        {
            get => GetValue(DisableMaximizeProperty);
            set => SetValue(DisableMaximizeProperty, value);
        }

        public bool DisableMinimize
        {
            get => GetValue(DisableMinimizeProperty);
            set => SetValue(DisableMinimizeProperty, value);
        }

        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public IImage? LogoIcon
        {
            get => GetValue(LogoIconProperty);
            set => SetValue(LogoIconProperty, value);
        }

        public IImage? MaximizeIcon
        {
            get => GetValue(MaximizeIconProperty);
            set => SetValue(MaximizeIconProperty, value);
        }

        public IImage? MinimizeIcon
        {
            get => GetValue(MinimizeIconProperty);
            set => SetValue(MinimizeIconProperty, value);
        }

        public IImage? RestoreIcon
        {
            get => GetValue(RestoreIconProperty);
            set => SetValue(RestoreIconProperty, value);
        }

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool DisplayWindowControls => OperatingSystem.IsWindows();

        private static void OnCloseIconChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender.CloseImage.Source = sender.CloseIcon;
        }

        private static void OnDisableChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender.CloseBtn.IsEnabled = !sender.GetValue(DisableCloseProperty);
            sender.MaximizeBtn.IsEnabled = !sender.GetValue(DisableMaximizeProperty);
            sender.MinimizeBtn.IsEnabled = !sender.GetValue(DisableMinimizeProperty);
        }

        private static void OnLogoIconChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender.AppLogo.Source = sender.LogoIcon;
        }

        private static void OnMaximizeIconChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender._state == WindowState.Normal)
                sender.MaximizeImage.Source = sender.MaximizeIcon;
        }

        private static void OnMinimizeIconChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender.MinimizeImage.Source = sender.MinimizeIcon;
        }

        private static void OnRestoreIconChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender._state == WindowState.Maximized)
                sender.MaximizeImage.Source = sender.RestoreIcon;
        }

        private static void OnTitleChanged(WindowTitlebar sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender.TitleRun.Text = sender.Title;
        }

        private void Close_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if(TopLevel.GetTopLevel(this) is Window window)
                window.Close();
        }

        private void MaximizeButton_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is not Window window) return;
            
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
                _state = WindowState.Maximized;
                MaximizeImage.Source = RestoreIcon;
            }
            else if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                _state = WindowState.Normal;
                MaximizeImage.Source = MaximizeIcon;
            }
        }

        private void Minimize_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is not Window window) return;

            window.WindowState = WindowState.Minimized;
            _state = WindowState.Minimized;
        }
    }
}