using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    public class ObservableConsoleLog(ConsoleLog consoleLog)
    {
        public string Body => ConsoleLog.Body;

        public ConsoleLog ConsoleLog { get; } = consoleLog;

        public string Header => ConsoleLog.IsReceived ? @$"[{ConsoleLog.UtcTime.ToLocalTime():HH:mm:ss}]" : @"> ";

        public bool IsError => ConsoleLog.IsError;
    }

    [Localizable(false)]
    public class RconPanel : ReactiveObject, IRefreshablePanel
    {
        public RconPanel(AppSetup setup, Launcher launcher)
        {
            _setup = setup;
            _launcher = launcher;
            SendCommand = ReactiveCommand.Create<string>(OnSendCommand, this.WhenAnyValue(x => x.CanSendCommand));
            CanBeOpened = _setup.Config is { ServerInstanceCount: > 0 };

            this.WhenAnyValue(x => x.SelectedConsole)
                .Subscribe((_) => OnConsoleSelectionChanged());
            
            LoadPanel();
        }
        private readonly AppSetup _setup;
        private readonly Launcher _launcher;
        private IConsole? _console;
        private List<IConanServerProcess> _servers = [];
        private int _selectedConsole;
        private bool _canSendCommand;
        private bool _canBeOpened;

        public List<string> AvailableConsoles { get; } = [];
        public ObservableCollection<ObservableConsoleLog> ConsoleLogs { get; private set; } = [];


        public string Icon => @"mdi-console-line";
        public string Label => Resources.PanelServerConsoles;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public int SelectedConsole
        {
            get => _selectedConsole;
            set => this.RaiseAndSetIfChanged(ref _selectedConsole, value);
        }

        public bool CanSendCommand
        {
            get => _canSendCommand;
            set => this.RaiseAndSetIfChanged(ref _canSendCommand, value);
        }

        public ReactiveCommand<string, Unit> SendCommand { get; }

        public void RefreshConsoleList(List<IConanServerProcess> servers)
        {
            _servers = servers;
            RefreshConsoleList();
        }

        public Task RefreshPanel()
        {
            CanBeOpened = _setup.Config is { ServerInstanceCount: > 0 };
            LoadPanel();
            return Task.CompletedTask;
        }

        private void LoadConsole(IConsole console)
        {
            if (_console != null)
                _console.LogReceived -= OnConsoleLogReceived;
            _console = console;
            if (_console != null)
            {
                _console.LogReceived += OnConsoleLogReceived;
                ConsoleLogs.Clear();
                ConsoleLogs.AddRange(_console.Historic.Select(x => new ObservableConsoleLog(x)));
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
            _servers = _launcher.GetServerProcesses().ToList();
            for (int i = 0; i < _setup.Config.ServerInstanceCount; i++)
            {
                var server = _servers.FirstOrDefault(x => x.Instance == i);
                AvailableConsoles.Add(server is { RConPort: > 0, State: ProcessState.ONLINE }
                    ? $"RCON - {server.Title} ({server.Instance}) - {IPAddress.Loopback}:{server.RConPort}"
                    : $"Unavailable - Instance {i}");
            }
            RefreshValidity();
        }

        private void RefreshValidity()
        {
            var valid = _servers.Any(server => server.Instance == SelectedConsole && server is { RConPort: > 0, State: ProcessState.ONLINE });
            CanSendCommand = valid;
            if (valid)
                LoadConsole(_launcher.GetServerConsole(SelectedConsole));
        }

    }
}