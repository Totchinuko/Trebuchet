using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for WindowTitlebar.xaml
    /// </summary>
    public partial class WindowTitlebar : UserControl
    {
        public static readonly DependencyProperty CloseIconProperty = DependencyProperty.Register(
            "CloseIcon",
            typeof(ImageSource),
            typeof(WindowTitlebar),
            new PropertyMetadata(null, new PropertyChangedCallback(OnCloseIconChanged))
            );

        public static readonly DependencyProperty DisableCloseProperty = DependencyProperty.Register(
            "DisableClose",
            typeof(bool),
            typeof(WindowTitlebar),
            new PropertyMetadata(false, new PropertyChangedCallback(OnDisableChanged))
            );

        public static readonly DependencyProperty DisableMaximizeProperty = DependencyProperty.Register(
            "DisableMaximize",
            typeof(bool),
            typeof(WindowTitlebar),
            new PropertyMetadata(false, new PropertyChangedCallback(OnDisableChanged))
            );

        public static readonly DependencyProperty DisableMinimizeProperty = DependencyProperty.Register(
            "DisableMinimize",
            typeof(bool),
            typeof(WindowTitlebar),
            new PropertyMetadata(false, new PropertyChangedCallback(OnDisableChanged))
            );

        public static readonly DependencyProperty MaximizeIconProperty = DependencyProperty.Register(
            "MaximizeIcon",
            typeof(ImageSource),
            typeof(WindowTitlebar),
            new PropertyMetadata(null, new PropertyChangedCallback(OnMaximizeIconChanged))
            );

        public static readonly DependencyProperty MinimizeIconProperty = DependencyProperty.Register(
            "MinimizeIcon",
            typeof(ImageSource),
            typeof(WindowTitlebar),
            new PropertyMetadata(null, new PropertyChangedCallback(OnMinimizeIconChanged))
            );

        public static readonly DependencyProperty RestoreIconProperty = DependencyProperty.Register(
            "RestoreIcon",
            typeof(ImageSource),
            typeof(WindowTitlebar),
            new PropertyMetadata(null, new PropertyChangedCallback(OnRestoreIconChanged))
            );

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(WindowTitlebar),
            new PropertyMetadata(null, new PropertyChangedCallback(OnTitleChanged))
            );

        private WindowState _state;

        public WindowTitlebar()
        {
            InitializeComponent();
        }

        public ImageSource CloseIcon
        {
            get => (ImageSource)GetValue(CloseIconProperty);
            set => SetValue(CloseIconProperty, value);
        }

        public bool DisableClose
        {
            get => (bool)GetValue(DisableCloseProperty);
            set => SetValue(DisableCloseProperty, value);
        }

        public bool DisableMaximize
        {
            get => (bool)GetValue(DisableMaximizeProperty);
            set => SetValue(DisableMaximizeProperty, value);
        }

        public bool DisableMinimize
        {
            get => (bool)GetValue(DisableMinimizeProperty);
            set => SetValue(DisableMinimizeProperty, value);
        }

        public ImageSource MaximizeIcon
        {
            get => (ImageSource)GetValue(MaximizeIconProperty);
            set => SetValue(MaximizeIconProperty, value);
        }

        public ImageSource MinimizeIcon
        {
            get => (ImageSource)GetValue(MinimizeIconProperty);
            set => SetValue(MinimizeIconProperty, value);
        }

        public ImageSource RestoreIcon
        {
            get => (ImageSource)GetValue(RestoreIconProperty);
            set => SetValue(RestoreIconProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnCloseIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WindowTitlebar titleBar)
                titleBar.CloseImage.Source = (ImageSource)e.NewValue;
        }

        private static void OnDisableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WindowTitlebar bar)
            {
                bar.CloseBtn.IsEnabled = !(bool)bar.GetValue(DisableCloseProperty);
                bar.MaximizeBtn.IsEnabled = !(bool)bar.GetValue(DisableMaximizeProperty);
                bar.MinimizeBtn.IsEnabled = !(bool)bar.GetValue(DisableMinimizeProperty);
            }
        }

        private static void OnMaximizeIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WindowTitlebar titleBar)
                if (titleBar._state == WindowState.Normal)
                    titleBar.MaximizeImage.Source = (ImageSource)e.NewValue;
        }

        private static void OnMinimizeIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WindowTitlebar titleBar)
                titleBar.MinimizeImage.Source = (ImageSource)e.NewValue;
        }

        private static void OnRestoreIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WindowTitlebar titleBar)
                if (titleBar._state == WindowState.Maximized)
                    titleBar.MaximizeImage.Source = (ImageSource)e.NewValue;
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WindowTitlebar titleBar)
                titleBar.TitleRun.Text = (string)e.NewValue;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                Window.GetWindow(this).DragMove();
        }

        private void Close_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void MaximizeButton_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this).WindowState == WindowState.Normal)
            {
                Window.GetWindow(this).WindowState = WindowState.Maximized;
                _state = WindowState.Maximized;
                Window.GetWindow(this).Padding = new Thickness(10);
                MaximizeImage.Source = (ImageSource)GetValue(RestoreIconProperty);
            }
            else if (Window.GetWindow(this).WindowState == WindowState.Maximized)
            {
                Window.GetWindow(this).WindowState = WindowState.Normal;
                _state = WindowState.Normal;
                Window.GetWindow(this).Padding = new Thickness(0);
                MaximizeImage.Source = (ImageSource)GetValue(MaximizeIconProperty);
            }
        }

        private void Minimize_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
            _state = WindowState.Minimized;
        }
    }
}