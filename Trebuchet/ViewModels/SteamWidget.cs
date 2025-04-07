using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using ReactiveUI;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class SteamWidget : ReactiveObject, ITinyRecipient<BlockedTaskStateChanged>
    {
        private readonly Steam _steam;
        private readonly TaskBlocker _taskBlocker;
        private readonly object _descriptionLock = new();
        private readonly object _progressLock;
        private readonly DispatcherTimer _timer;
        private double _progress;
        private string _description = string.Empty;
        private bool _isConnected;
        private bool _isIndeterminate;
        private double _progressBar;
        private bool _canConnect;
        private bool _isLoading;

        public SteamWidget(
            ITinyMessengerHub messenger, 
            IProgressCallback<double> progress, 
            Steam steam, 
            TaskBlocker taskBlocker)
        {
            _steam = steam;
            _taskBlocker = taskBlocker;
            progress.ProgressChanged += OnProgressChanged;

            messenger.Subscribe(this);

            _progressLock = new object();
            _steam.Connected += OnSteamConnected;
            _steam.Disconnected += OnSteamDisconnected;

            CancelCommand = ReactiveCommand.Create(OnCancel);
            ConnectCommand = ReactiveCommand.Create(OnConnect, this.WhenAnyValue(x => x.CanConnect));

            this.WhenAnyValue(x => x.Progress).Select(x => x == 0.0).ToProperty(this, x => x.IsIndeterminate);

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, Tick);
        }

        private void OnProgressChanged(object? sender, double e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Progress = e;
            });
        }


        public ReactiveCommand<Unit,Unit> CancelCommand { get; }
        public ReactiveCommand<Unit,Unit> ConnectCommand { get; }

        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => this.RaiseAndSetIfChanged(ref _isIndeterminate, value);
        }

        public double Progress
        {
            get => _progressBar;
            set => this.RaiseAndSetIfChanged(ref _progressBar, value);
        }

        public bool CanConnect
        {
            get => _canConnect;
            set => this.RaiseAndSetIfChanged(ref _canConnect, value);
        }

        public void Receive(BlockedTaskStateChanged message)
        {
            if (message.Type.GetType() != typeof(SteamDownload)) return;

            if (!message.Value)
            {
                SetDescription(string.Empty);
                _timer.Stop();
                Progress = 0;
            }
            else
            {
                SetDescription(message.Type.Label);
                _timer.Start();
                Progress = 0;
            }
        }

        public void Report(double value)
        {
            lock (_progressLock)
            {
                _progress = Math.Clamp(value, 0, 1);
            }
        }

        public void SetDescription(string description)
        {
            lock (_descriptionLock)
            {
                Description = description;
                IsLoading = !string.IsNullOrEmpty(description);
            }
        }

        public void Start(string description)
        {
            SetDescription(description);
            _timer.Start();
        }

        private void OnCancel()
        {
            _taskBlocker.Cancel<SteamDownload>();
            SetDescription("Canceling...");
        }

        private async void OnConnect()
        {
            if (!IsConnected)
            {
                CanConnect = false;
                await _steam.Connect();
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            lock (_progressLock)
            {
                Progress = _progress;
            }
        }
        
        private void OnSteamDisconnected(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsConnected = false;
                CanConnect = true;
            });
        }

        private void OnSteamConnected(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsConnected = true;
                CanConnect = true;
            });
        }
    }
}