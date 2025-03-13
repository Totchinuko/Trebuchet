using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TrebuchetLib;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for DirectoryFinder.xaml
    /// </summary>
    public partial class DirectoryFinder : UserControl
    {
        // public static readonly DependencyProperty CreateDefaultFolderProperty = DependencyProperty.Register("CreateDefaultFolder", typeof(bool), typeof(DirectoryFinder), new PropertyMetadata(false));
        // public static readonly DependencyProperty DefaultFolderProperty = DependencyProperty.Register("DefaultFolder", typeof(string), typeof(DirectoryFinder), new PropertyMetadata(string.Empty));
        // public static readonly DependencyProperty PathProperty = DependencyProperty.Register("Path", typeof(string), typeof(DirectoryFinder), new PropertyMetadata(string.Empty));

        public DirectoryFinder()
        {
            InitializeComponent();
        }
        //
        // public bool CreateDefaultFolder
        // {
        //     get => (bool)GetValue(CreateDefaultFolderProperty);
        //     set => SetValue(CreateDefaultFolderProperty, value);
        // }
        //
        // public string DefaultFolder
        // {
        //     get => (string)GetValue(DefaultFolderProperty);
        //     set => SetValue(DefaultFolderProperty, value);
        // }
        //
        // public string Path
        // {
        //     get => (string)GetValue(PathProperty);
        //     set => SetValue(PathProperty, value);
        // }
        //
        // private void FindButton_MouseDown(object sender, RoutedEventArgs e)
        // {
        //     string appDir = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception("App is installed in an invalid directory");
        //     string defaultFolder = string.Empty;
        //     if (!string.IsNullOrEmpty(DefaultFolder))
        //     {
        //         defaultFolder = DefaultFolder.Replace("%APP_DIRECTORY%", appDir);
        //         if (CreateDefaultFolder && !Directory.Exists(defaultFolder))
        //             Tools.CreateDir(defaultFolder);
        //     }
        //
        //     if (string.IsNullOrEmpty(defaultFolder) || !Directory.Exists(defaultFolder))
        //         defaultFolder = appDir;
        //
        //     System.Windows.Forms.FolderBrowserDialog dlg = new();
        //     dlg.InitialDirectory = defaultFolder;
        //     System.Windows.Forms.DialogResult result = dlg.ShowDialog(NativeWindow.GetIWin32Window(this));
        //     if (result != System.Windows.Forms.DialogResult.Cancel)
        //     {
        //         Path = dlg.SelectedPath;
        //     }
        // }
    }
}