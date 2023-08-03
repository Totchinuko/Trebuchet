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
    public partial class WindowTitlebar : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty CloseIconProperty = DependencyProperty.Register("CloseIcon", typeof(ImageSource), typeof(WindowTitlebar));
        public static readonly DependencyProperty MaximizeIconProperty = DependencyProperty.Register("MaximizeIcon", typeof(ImageSource), typeof(WindowTitlebar));
        public static readonly DependencyProperty RestoreIconProperty = DependencyProperty.Register("RestoreIcon", typeof(ImageSource), typeof(WindowTitlebar));
        public static readonly DependencyProperty MinimizeIconProperty = DependencyProperty.Register("MinimizeIcon", typeof(ImageSource), typeof(WindowTitlebar));

        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSource CloseIcon
        {
            get => (ImageSource)GetValue(CloseIconProperty);
            set => SetValue(CloseIconProperty, value);
        }

        public ImageSource MaximizeIcon
        {
            get => (ImageSource)GetValue(MaximizeIconProperty);
            set => SetValue(MaximizeIconProperty, value);
        }

        public ImageSource RestoreIcon
        {
            get => (ImageSource)GetValue(RestoreIconProperty);
            set => SetValue(RestoreIconProperty, value);
        }

        public ImageSource MinimizeIcon
        {
            get => (ImageSource)GetValue(MinimizeIconProperty);
            set => SetValue(MinimizeIconProperty, value);
        }

        public string Title
        {
            get => Window.GetWindow(this).Title;
            set => Window.GetWindow(this).Title = value;
        }

        public ImageSource Icon
        {
            get => Window.GetWindow(this).Icon;
            set => Window.GetWindow(this).Icon = value;
        }

        public WindowState WindowState
        {
            get => Window.GetWindow(this).WindowState;
            set => Window.GetWindow(this).WindowState = value;
        }

        public WindowTitlebar()
        {
            InitializeComponent();            
            DataContext = this;
        }

        private void Minimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WindowState"));
        }

        private void MaximizeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this).WindowState == WindowState.Normal)
            {
                Window.GetWindow(this).WindowState = WindowState.Maximized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WindowState"));
                Window.GetWindow(this).Padding = new Thickness(10);
            }
            else if (Window.GetWindow(this).WindowState == WindowState.Maximized)
            {
                Window.GetWindow(this).WindowState = WindowState.Normal;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WindowState"));
                Window.GetWindow(this).Padding = new Thickness(0);
            }
        }

        private void Close_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                Window.GetWindow(this).DragMove();
        }
    }
}
