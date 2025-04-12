using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for ConsoleField.xaml
    /// </summary>
    public partial class ConsoleField : UserControl
    {
        public static readonly StyledProperty<ICommand> CommandProperty = AvaloniaProperty.Register<ConsoleField, ICommand>(nameof(Command));
        
        public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<ConsoleField, int>(nameof(MaxLength));

        public static readonly StyledProperty<IBrush> PlaceholderBrushProperty =
            AvaloniaProperty.Register<ConsoleField, IBrush>(nameof(PlaceholderBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)));
        
        public static readonly StyledProperty<string> PlaceholderProperty = AvaloniaProperty.Register<ConsoleField, string>(nameof(Placeholder));

        public ConsoleField()
        {
            CommandProperty.Changed.AddClassHandler<ConsoleField>(OnCommandChanged);
            PlaceholderBrushProperty.Changed.AddClassHandler<ConsoleField>(OnPlaceHolderChanged);
            PlaceholderProperty.Changed.AddClassHandler<ConsoleField>(OnPlaceHolderChanged);
            InitializeComponent();
        }

        public ICommand Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        
        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }
        
        public string Placeholder
        {
            get => GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }
        
        public IBrush PlaceholderBrush
        {
            get => GetValue(PlaceholderBrushProperty);
            set => SetValue(PlaceholderBrushProperty, value);
        }
        
        private static void OnCommandChanged(ConsoleField sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ICommand oldCommand)
                oldCommand.CanExecuteChanged -= sender.Command_CanExecuteChanged;
            sender.Command_CanExecuteChanged(sender.Command, EventArgs.Empty);
            if (e.NewValue is ICommand newCommand)
                newCommand.CanExecuteChanged += sender.Command_CanExecuteChanged;
        }
        
        private static void OnPlaceHolderChanged(ConsoleField sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (!sender.IsFocused)
                sender.Console_LostFocus(sender.Console, new RoutedEventArgs());
        }
        
        private void Command_CanExecuteChanged(object? sender, EventArgs e)
        {
            IsEnabled = Command.CanExecute(null);
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
                Command.Execute(Console.Text);
                Console.Text = string.Empty;
            }
        }
    }
}