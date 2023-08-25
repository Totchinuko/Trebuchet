using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for ConsoleField.xaml
    /// </summary>
    public partial class ConsoleField : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(ConsoleField), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register("MaxLength", typeof(int), typeof(ConsoleField), new PropertyMetadata(0));

        public static readonly DependencyProperty PlaceholderBrushProperty = DependencyProperty.Register("PlaceholderBrush", typeof(Brush), typeof(ConsoleField), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), OnPlaceHolderChanged));

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register("Placeholder", typeof(string), typeof(ConsoleField), new PropertyMetadata(string.Empty, OnPlaceHolderChanged));

        public ConsoleField()
        {
            InitializeComponent();
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set
            {
                if (Command != null)
                    Command.CanExecuteChanged -= Command_CanExecuteChanged;
                SetValue(CommandProperty, value);
                if (Command != null)
                {
                    IsEnabled = Command.CanExecute(null);
                    Command.CanExecuteChanged += Command_CanExecuteChanged;
                }
            }
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public Brush PlaceholderBrush
        {
            get => (Brush)GetValue(PlaceholderBrushProperty);
            set => SetValue(PlaceholderBrushProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConsoleField consoleField)
                consoleField.Command_CanExecuteChanged(consoleField.Command, EventArgs.Empty);
        }

        private static void OnPlaceHolderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConsoleField consoleField && !consoleField.IsFocused)
                consoleField.Console_LostFocus(consoleField.Console, new RoutedEventArgs());
        }

        private void Command_CanExecuteChanged(object? sender, EventArgs e)
        {
            IsEnabled = Command?.CanExecute(null) ?? false;
        }

        private void Console_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Console.Text == Placeholder)
            {
                Console.Text = "";
                Console.Foreground = Foreground;
            }
        }

        private void Console_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Console.Text) || Console.Text == Placeholder)
            {
                Console.Text = Placeholder;
                Console.Foreground = PlaceholderBrush;
            }
        }

        private void Console_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(Console.Text))
            {
                Command?.Execute(Console.Text);
                Console.Text = string.Empty;
            }
        }
    }
}