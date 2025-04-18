using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    [Localizable(false)]
    public class ConsolePanel : ReactiveObject, IRefreshablePanel, ITickingPanel
    {
        public ConsolePanel(AppSetup setup, Launcher launcher)
        {
            _setup = setup;
            _launcher = launcher;

            AdjustConsoleListIfNeeded();
            _console = ConsoleList[0];
            
            var canSendCommand = this.WhenAnyValue(x => x.Console.CanSend, x => x.CommandField,
                (c, f) => c && !string.IsNullOrEmpty(f));
            
            SendCommand = ReactiveCommand.CreateFromTask(OnSendCommand, canSendCommand);
            CanBeOpened = _setup.Config is { ServerInstanceCount: > 0 };

            OpenPopup = ReactiveCommand.Create<Unit>((_) => PopupOpen = true);
        }

        private readonly AppSetup _setup;
        private readonly Launcher _launcher;
        private MixedConsoleViewModel _console;
        private bool _canBeOpened;
        private string _commandField = string.Empty;
        private bool _popupOpen;

        public string Icon => @"mdi-console-line";
        public string Label => Resources.PanelServerConsoles;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public string CommandField
        {
            get => _commandField;
            set => this.RaiseAndSetIfChanged(ref _commandField, value);
        }

        public MixedConsoleViewModel Console
        {
            get => _console;
            set => this.RaiseAndSetIfChanged(ref _console, value);
        }

        public bool PopupOpen
        {
            get => _popupOpen;
            set => this.RaiseAndSetIfChanged(ref _popupOpen, value);
        }

        public ObservableCollectionExtended<MixedConsoleViewModel> ConsoleList { get; } = [];

        public ReactiveCommand<Unit, Unit> SendCommand { get; }
        public ReactiveCommand<Unit,Unit> OpenPopup { get; }

        public Task TickPanel()
        {
            var servers = _launcher.GetServerProcesses().ToList();
            AdjustConsoleListIfNeeded();
            foreach (var s in servers)
                ConsoleList[s.Instance].Process = s;

            return Task.CompletedTask;
        }

        public Task RefreshPanel()
        {
            CanBeOpened = _setup.Config is { ServerInstanceCount: > 0 };
            return Task.CompletedTask;
        }

        private void AdjustConsoleListIfNeeded()
        {
            int count = Math.Max(1, _setup.Config.ServerInstanceCount);
            if (ConsoleList.Count >= count) return;
            for (var i = ConsoleList.Count; i < count; i++)
            {
                var console = new MixedConsoleViewModel(i);
                console.ConsoleSelected += OnConsoleSelected;
                ConsoleList.Add(console);
            }
        }

        private void OnConsoleSelected(object? sender, int e)
        {
            if (sender is MixedConsoleViewModel console)
                Console = console;
            PopupOpen = false;
        }

        private async Task OnSendCommand()
        {
            var command = CommandField;
            CommandField = string.Empty;
            await _console.Send(command);
        }
    }
}