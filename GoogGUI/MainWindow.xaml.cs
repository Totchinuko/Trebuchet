using System;
using System.Collections.Generic;
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

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Minimize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                MaximizeButton.ButtonIcon = new BitmapImage(new Uri("/GoogGUI;component/Icons/Restore.png", UriKind.Relative));
                MainBorder.Padding = new Thickness(10);
            }
            else if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeButton.ButtonIcon = new BitmapImage(new Uri("/GoogGUI;component/Icons/Maximize.png", UriKind.Relative));
                MainBorder.Padding = new Thickness(0);
            }
        }

        private void Close_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
