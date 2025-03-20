using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.Panels
{
    public class ObservableConsoleLog(ConsoleLog consoleLog)
    {
        public string Body => ConsoleLog.Body;

        public ConsoleLog ConsoleLog { get; } = consoleLog;

        public string Header => ConsoleLog.IsReceived ? $"[{ConsoleLog.UtcTime.ToLocalTime():HH:mm:ss}]" : "> ";

        public bool IsError => ConsoleLog.IsError;
    }

    public class RconPanel : Panel,
            IRecipient<ServerProcessStateChanged>
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private IConsole? _console;
        private int _selectedConsole;
        private List<ProcessServerDetails> _servers = new List<ProcessServerDetails>();

        public RconPanel() : base("RconPanel")
        {
            SendCommand = new SimpleCommand(OnSendCommand, false);

            StrongReferenceMessenger.Default.Register<ServerProcessStateChanged>(this);

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

        public override bool CanExecute(object? parameter)
        {
            return _config is { IsInstallPathValid: true, ServerInstanceCount: > 0 };
        }

        public void Receive(ServerProcessStateChanged message)
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
            Dispatcher.UIThread.Invoke(() =>
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
            _servers = StrongReferenceMessenger.Default.Send<ProcessServerDetailsRequest>().Response;
            for (int i = 0; i < _config.ServerInstanceCount; i++)
            {
                var server = _servers.FirstOrDefault(x => x.Instance == i);
                AvailableConsoles.Add(server is { RconPort: > 0, State: ProcessState.ONLINE }
                    ? $"RCON - {server.Title} ({server.Instance}) - {IPAddress.Loopback}:{server.RconPort}"
                    : $"Unavailable - Instance {i}");
            }
            OnPropertyChanged(nameof(AvailableConsoles));
            RefreshValidity();
        }

        private void RefreshValidity()
        {
            var valid = _servers.Any(server => server.Instance == _selectedConsole && server is { RconPort: > 0, State: ProcessState.ONLINE });
            SendCommand.Toggle(valid);
            if (valid)
                LoadConsole(StrongReferenceMessenger.Default.Send(new ServerConsoleRequest(_selectedConsole)).Response);
        }
    }
}