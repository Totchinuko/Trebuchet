using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TrebuchetLib;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for DirectoryFinder.xaml
    /// </summary>
    public partial class DirectoryFinder : UserControl
    {
        public static readonly StyledProperty<bool> CreateDefaultFolderProperty = AvaloniaProperty.Register<DirectoryFinder, bool>(nameof(CreateDefaultFolder));
        public static readonly StyledProperty<string> DefaultFolderProperty = AvaloniaProperty.Register<DirectoryFinder, string>(nameof(DefaultFolder));
        public static readonly StyledProperty<string> PathProperty = AvaloniaProperty.Register<DirectoryFinder, string>(nameof(Path));

        public DirectoryFinder()
        {
            InitializeComponent();
        }
        
        public bool CreateDefaultFolder
        {
            get => GetValue(CreateDefaultFolderProperty);
            set => SetValue(CreateDefaultFolderProperty, value);
        }
        
        public string DefaultFolder
        {
            get => GetValue(DefaultFolderProperty);
            set => SetValue(DefaultFolderProperty, value);
        }
        
        public string Path
        {
            get => GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
        
        private async void FindButton_MouseDown(object sender, RoutedEventArgs e)
        {
            string appDir = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception("App is installed in an invalid directory");
            string defaultFolder = string.Empty;
            if (!string.IsNullOrEmpty(DefaultFolder))
            {
                defaultFolder = DefaultFolder.Replace("%APP_DIRECTORY%", appDir);
                if (CreateDefaultFolder && !Directory.Exists(defaultFolder))
                    Tools.CreateDir(defaultFolder);
            }
        
            if (string.IsNullOrEmpty(defaultFolder) || !Directory.Exists(defaultFolder))
                defaultFolder = appDir;

            var toplevel = TopLevel.GetTopLevel(this);
            if (toplevel == null) return;

            var folder = await toplevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false,
                SuggestedStartLocation = await toplevel.StorageProvider.TryGetFolderFromPathAsync(defaultFolder)
            });

            if (folder.Count == 0) return;
            Path = folder[0].Path.ToString();
        }
    }
}