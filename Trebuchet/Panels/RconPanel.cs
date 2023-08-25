using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using TrebuchetLib;

namespace Trebuchet
{
    public class ObservableConsoleLog
    {
        public ObservableConsoleLog(ConsoleLog consoleLog)
        {
            ConsoleLog = consoleLog;
        }

        public string Body => ConsoleLog.Body;

        public ConsoleLog ConsoleLog { get; }

        public string Header => ConsoleLog.IsReceived ? $"[{ConsoleLog.UtcTime.ToLocalTime():HH:mm:ss}]" : "> ";

        public bool IsError => ConsoleLog.IsError;
    }

    public class RconPanel : Panel,
            IRecipient<ProcessMessage>
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private IConsole? _console;
        private int _selectedConsole = 0;
        private List<ServerInstanceInformation> _servers = new List<ServerInstanceInformation>();

        public RconPanel()
        {
            SendCommand = new SimpleCommand(OnSendCommand, false);

            StrongReferenceMessenger.Default.Register<ProcessStartedMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessStoppedMessage>(this);

            LoadPanel();
        }

        public List<string> AvailableConsoles { get; } = new List<string>();

        public ObservableCollection<ObservableConsoleLog> ConsoleLogs { get; private set; } = new ObservableCollection<ObservableConsoleLog>();

        public int SelectedConsole
        {
            get => _selectedConsole;
            set
            {
                _selectedConsole = value;
                OnConsoleSelectionChanged();
                OnPropertyChanged(nameof(SelectedConsole));
            }
        }

        public SimpleCommand SendCommand { get; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["RconPanel"];

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                   _config.ServerInstanceCount > 0;
        }

        public void Receive(ProcessMessage message)
        {
            RefreshConsoleList();
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        private void LoadConsole(IConsole console)
        {
            if (_console != null)
                _console.LogReceived -= OnConsoleLogReceived;
            _console = console;
            if (_console != null)
            {
                _console.LogReceived += OnConsoleLogReceived;
                ConsoleLogs = new ObservableCollection<ObservableConsoleLog>(_console.Historic.Select(x => new ObservableConsoleLog(x)));
                OnPropertyChanged(nameof(ConsoleLogs));
            }
        }

        private void LoadPanel()
        {
            RefreshConsoleList();
        }

        private void OnConsoleLogReceived(object? sender, ConsoleLogEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConsoleLogs.Add(new ObservableConsoleLog(e.ConsoleLog));
                if (ConsoleLogs.Count > 200)
                    ConsoleLogs.RemoveAt(0);
            });
        }

        private void OnConsoleSelectionChanged()
        {
            ConsoleLogs.Clear();
            RefreshValidity();
        }

        private void OnSendCommand(object? obj)
        {
            if (obj is string command)
                _console?.SendCommand(command);
        }

        private void RefreshConsoleList()
        {
            AvailableConsoles.Clear();
            _servers = StrongReferenceMessenger.Default.Send<ServerInfoRequest>().Response;
            for (int i = 0; i < _config.ServerInstanceCount; i++)
            {
                var server = _servers.Where(x => x.Instance == i).FirstOrDefault();
                if (server != null && server.RconPort > 0)
                    AvailableConsoles.Add($"RCON - {server.Title} ({server.Instance}) - {IPAddress.Loopback}:{server.RconPort}");
                else
                    AvailableConsoles.Add($"Unavailable - Instance {i}");
            }
            OnPropertyChanged(nameof(AvailableConsoles));
            RefreshValidity();
        }

        private void RefreshValidity()
        {
            var valid = _servers.Any(server => server.Instance == _selectedConsole);
            SendCommand.Toggle(valid);
            if (valid)
                LoadConsole(StrongReferenceMessenger.Default.Send(new ServerConsoleRequest(_selectedConsole)).Response);
        }
    }
}