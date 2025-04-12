using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Trebuchet.ViewModels.Panels;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for ConsoleLog.xaml
    /// </summary>
    public partial class ConsoleLog : UserControl
    {
        public static readonly StyledProperty<ObservableCollection<ObservableConsoleLog>> ConsoleLogsProperty =
            AvaloniaProperty.Register<ConsoleLog, ObservableCollection<ObservableConsoleLog>>(nameof(ConsoleLogs));

        public ConsoleLog()
        {
            ConsoleLogsProperty.Changed.AddClassHandler<ConsoleLog>(OnLogChanged);
            InitializeComponent();
        }

        public ObservableCollection<ObservableConsoleLog> ConsoleLogs
        {
            get => GetValue(ConsoleLogsProperty);
            set => SetValue(ConsoleLogsProperty, value);
        }
        
        private static void OnLogChanged(ConsoleLog sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ObservableCollection<ObservableConsoleLog> oldCollection)
                oldCollection.CollectionChanged -= sender.ConsoleLogs_CollectionChanged;
            sender.ConsoleLogs_CollectionChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (e.NewValue is ObservableCollection<ObservableConsoleLog> newCollection)
                newCollection.CollectionChanged += sender.ConsoleLogs_CollectionChanged;
        }
        
        private void ConsoleLogs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogScrollViewer.ScrollToEnd();
        }
    }
}