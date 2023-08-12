using GoogLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogGUI.Controls
{
    /// <summary>
    /// Interaction logic for StringListEditor.xaml
    /// </summary>
    public partial class StringListEditor : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values",
            typeof(ObservableCollection<string>),
            typeof(StringListEditor),
            new PropertyMetadata(
                new ObservableCollection<string>())
            );

        public StringListEditor()
        {
            DeleteCommand = new SimpleCommand(OnInstanceDelete);
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand DeleteCommand { get; private set; }

        public ObservableCollection<string> Values
        {
            get => (ObservableCollection<string>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Values == null)
                Values = new ObservableCollection<string>();
            Values.Add(string.Empty);
        }

        private void OnInstanceDelete(object? obj)
        {
            if (Values.Count <= 1) return;
            if (obj is string value)
                Values.Remove(value);
            InstanceList.ItemsSource = Values;
        }
    }
}