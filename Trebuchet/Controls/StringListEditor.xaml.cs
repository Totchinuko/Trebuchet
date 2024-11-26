using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for StringListEditor.xaml
    /// </summary>
    public partial class StringListEditor : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values",
            typeof(TrulyObservableCollection<ObservableString>),
            typeof(StringListEditor),
            new PropertyMetadata(
                new TrulyObservableCollection<ObservableString>())
            );

        public StringListEditor()
        {
            DeleteCommand = new SimpleCommand(OnInstanceDelete);
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand DeleteCommand { get; private set; }

        public TrulyObservableCollection<ObservableString> Values
        {
            get => (TrulyObservableCollection<ObservableString>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Values == null)
                Values = new TrulyObservableCollection<ObservableString>();
            Values.Add(new ObservableString());
        }

        private void OnInstanceDelete(object? obj)
        {
            if (Values.Count == 0) return;
            if (obj is ObservableString value)
                Values.Remove(value);
            InstanceList.ItemsSource = Values;
        }

        private void TextBox_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Values = Values;
        }
    }
}