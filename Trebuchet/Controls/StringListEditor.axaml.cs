using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Trebuchet.ViewModels;
using TrebuchetUtils;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for StringListEditor.xaml
    /// </summary>
    public partial class StringListEditor : UserControl
    {
        public static readonly StyledProperty<TrulyObservableCollection<ObservableString>> ValuesProperty =
            AvaloniaProperty.Register<StringListEditor, TrulyObservableCollection<ObservableString>>(nameof(Values), defaultValue: []);

        public StringListEditor()
        {
            DeleteCommand = new SimpleCommand().Subscribe(OnInstanceDelete);
            InitializeComponent();
        }
        
        public ICommand DeleteCommand { get; private set; }
        
        public TrulyObservableCollection<ObservableString> Values
        {
            get => (TrulyObservableCollection<ObservableString>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }
        
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Values.Add(new ObservableString());
        }
        
        private void OnInstanceDelete(object? obj)
        {
            if (Values.Count == 0) return;
            if (obj is ObservableString value)
                Values.Remove(value);
            InstanceList.ItemsSource = Values;
        }
        
        private void TextBox_PreviewLostKeyboardFocus(object? sender, RoutedEventArgs routedEventArgs)
        {
            Values = Values;
        }
    }
}