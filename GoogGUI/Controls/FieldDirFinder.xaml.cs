using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for FieldDirFinder.xaml
    /// </summary>
    public partial class FieldDirFinder : UserControl, INotifyPropertyChanged, IGuiField
    {
        private string _default = string.Empty;
        private string _fieldName = string.Empty;
        private string _path = string.Empty;
        private string _property = string.Empty;

        public FieldDirFinder(string name)
        {
            FieldName = name;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<object?>? ValueChanged;

        public string FieldName { get => _fieldName; set => _fieldName = value; }
        public bool IsDefault => Path.Equals(_default);
        public string Path { get => _path; set => _path = value; }
        public string Property => _property;
        public object? GetField()
        {
            return Path;
        }

        public void ResetToDefault()
        {
            Path = string.Empty;
        }

        public void SetField(string property, object? value, object? defaultValue)
        {
            if (value == null || value is not string path)
                return;
            if (defaultValue == null || defaultValue is not string defaultPath)
                return;

            _property = property;
            Path = path;
            _default = defaultPath;
        }

        public bool Validate()
        {
            return Directory.Exists(Path);
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        protected virtual void OnValueChanged(object? value)
        {
            ValueChanged?.Invoke(this, value);
        }
        private void PathField_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnValueChanged(_path);
        }

        private void SearchFolder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new ();
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(NativeWindow.GetIWin32Window(this));
            if(result != System.Windows.Forms.DialogResult.Cancel)
            {
                Path = dlg.SelectedPath;
                OnValueChanged(_path);
                OnPropertyChanged("Path");
            }

        }
    }
}
