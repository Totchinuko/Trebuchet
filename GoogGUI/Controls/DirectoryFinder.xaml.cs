using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for DirectoryFinder.xaml
    /// </summary>
    public partial class DirectoryFinder : UserControl
    {
        public static readonly DependencyProperty PathProperty = DependencyProperty.Register("Path", typeof(string), typeof(DirectoryFinder), new PropertyMetadata(string.Empty));

        public string Path
        {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public DirectoryFinder()
        {
            InitializeComponent();
        }

        private void FindButton_MouseDown(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new();
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(NativeWindow.GetIWin32Window(this));
            if (result != System.Windows.Forms.DialogResult.Cancel)
            {
                Path = dlg.SelectedPath;
            }
        }
    }
}