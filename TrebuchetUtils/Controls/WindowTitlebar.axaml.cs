﻿using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace TrebuchetUtils.Controls
{
    /// <summary>
    /// Interaction logic for WindowTitlebar.xaml
    /// </summary>
    public partial class WindowTitlebar : UserControl
    {

        public static readonly StyledProperty<bool> DisableCloseProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(DisableClose));

        public static readonly StyledProperty<bool> DisableMaximizeProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(DisableMaximize));
        
        public static readonly StyledProperty<bool> DisableMinimizeProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(DisableMinimize));

        public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<WindowTitlebar, object?>(nameof(Header));

        public static readonly StyledProperty<IImage?> LogoIconProperty = AvaloniaProperty.Register<WindowTitlebar, IImage?>(nameof(LogoIcon));

        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<WindowTitlebar, string>(nameof(Title));
        
        public static readonly StyledProperty<bool> IsMaximizedProperty = AvaloniaProperty.Register<WindowTitlebar, bool>(nameof(IsMaximized));

        public WindowTitlebar()
        {
            DisableCloseProperty.Changed.AddClassHandler<WindowTitlebar>(OnDisableChanged);
            DisableMaximizeProperty.Changed.AddClassHandler<WindowTitlebar>(OnDisableChanged);
            DisableMinimizeProperty.Changed.AddClassHandler<WindowTitlebar>(OnDisableChanged);
            LogoIconProperty.Changed.AddClassHandler<WindowTitlebar>(OnLogoIconChanged);
            TitleProperty.Changed.AddClassHandler<WindowTitlebar>(OnTitleChanged);
            InitializeComponent();

        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (TopLevel.GetTopLevel(this) is not Window window)
                throw new ApplicationException("Title bar only function on desktop lifetime");
            window.PropertyChanged += OnWindowPropertyChanged;
            IsMaximized = window.WindowState == WindowState.Maximized;
        }

        private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "WindowState")
            {
                if (TopLevel.GetTopLevel(this) is not Window window) return;
                IsMaximized = window.WindowState == WindowState.Maximized;
            }
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

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool DisplayWindowControls => !Utils.UseOsChrome();

        public bool IsMaximized
        {
            get => GetValue(IsMaximizedProperty);
            set => SetValue(IsMaximizedProperty, value);
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
            }
            else if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
            }
        }

        private void Minimize_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is not Window window) return;

            window.WindowState = WindowState.Minimized;
        }
    }
}