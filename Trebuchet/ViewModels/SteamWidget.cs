using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib.Services;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels
{
    public class SteamWidget : BaseViewModel, ITinyRecipient<BlockedTaskStateChanged>
    {
        private readonly Steam _steam;
        private readonly TaskBlocker _taskBlocker;
        private readonly object _descriptionLock = new();
        private readonly object _progressLock;
        private string _description = string.Empty;
        private readonly DispatcherTimer _timer;
        private bool _isConnected;
        private double _progress;

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
            CancelCommand = new SimpleCommand().Subscribe(OnCancel);
            ConnectCommand = new SimpleCommand().Subscribe(OnConnect);

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, Tick);
        }

        private void OnProgressChanged(object? sender, double e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Progress = e;
            });
        }


        public SimpleCommand CancelCommand { get; }

        public SimpleCommand ConnectCommand { get; }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set => SetField(ref _isConnected, value);
        }

        public bool IsLoading => !string.IsNullOrEmpty(Description);

        public double Progress
        {
            get => _progress;
            set
            {
                if (SetField(ref _progress, value)) 
                    OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        public bool IsIndeterminate => Progress == 0;

        public bool CanExecute(bool displayError = true)
        {
            if (!IsConnected)
            {
                if (displayError)
                    new ErrorModal("Steam Error", "You must be connected to Steam for this operation.").OpenDialogue();
                return false;
            }
            return true;
        }

        public void Receive(BlockedTaskStateChanged message)
        {
            if (message.Type.GetType() != typeof(SteamDownload)) return;

            if (!message.Value)
            {
                SetDescription(string.Empty);
                _timer.Stop();
                _progress = 0;
            }
            else
            {
                SetDescription(message.Type.Label);
                _timer.Start();
                _progress = 0;
            }
            
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(IsIndeterminate));
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
                _description = description;
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        public void Start(string description)
        {
            SetDescription(description);
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsIndeterminate));
            _timer.Start();
        }

        private void OnCancel(object? obj)
        {
            _taskBlocker.Cancel<SteamDownload>();
            SetDescription("Canceling...");
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsIndeterminate));
        }

        private async void OnConnect(object? obj)
        {
            if (!IsConnected)
            {
                ConnectCommand.Toggle(false);
                await _steam.Connect();
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            lock (_progressLock)
            {
                Progress = _progress;
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }
        
        private void OnSteamDisconnected(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsConnected = false;
                OnPropertyChanged(nameof(IsConnected));
                ConnectCommand.Toggle(true);
            });
        }

        private void OnSteamConnected(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsConnected = true;
                OnPropertyChanged(nameof(IsConnected));
                ConnectCommand.Toggle(true);
            });
        }
    }
}