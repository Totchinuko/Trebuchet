using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
using TrebuchetLib;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for ConsoleLog.xaml
    /// </summary>
    public partial class ConsoleLog : UserControl
    {
        public static readonly DependencyProperty ConsoleLogsProperty = DependencyProperty.Register(
                       "ConsoleLogs", typeof(ObservableCollection<ObservableConsoleLog>), typeof(ConsoleLog), new PropertyMetadata(null, OnLogChanged));

        public ConsoleLog()
        {
            InitializeComponent();
        }

        public ObservableCollection<ObservableConsoleLog> ConsoleLogs
        {
            get => (ObservableCollection<ObservableConsoleLog>)GetValue(ConsoleLogsProperty);
            set => SetValue(ConsoleLogsProperty, value);
        }

        private static void OnLogChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConsoleLog consoleLog)
            {
                if (e.OldValue != null && e.OldValue is ObservableCollection<ObservableConsoleLog> oldCollection)
                    oldCollection.CollectionChanged -= consoleLog.ConsoleLogs_CollectionChanged;
                consoleLog.ConsoleLogs_CollectionChanged(consoleLog, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (e.NewValue != null && e.NewValue is ObservableCollection<ObservableConsoleLog> newCollection)
                    newCollection.CollectionChanged += consoleLog.ConsoleLogs_CollectionChanged;
            }
        }

        private void ConsoleLogs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogScrollViewer.ScrollToEnd();
        }
    }
}