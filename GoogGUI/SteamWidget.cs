using Goog;
using SteamKit2;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    public class SteamWidget : IProgress<double>, INotifyPropertyChanged
    {
        public const string SteamTask = "SteamDownloadTask";

        private CancellationTokenSource? _cts;
        private double _progress;
        private object _progressLock = new object();
        private SteamSession _steam;
        private DispatcherTimer _timer;

        public SteamWidget(SteamSession steam)
        {
            _steam = steam;
            _steam.Connected += OnConnected;
            _steam.Disconnected += OnDisconnected;
            _steam.LoggedOn += OnLoggedOn;

            _progressLock = new object();
            CancelCommand = new SimpleCommand(OnCancel);
            ConnectCommand = new SimpleCommand(OnConnect);
            App.TaskBlocker.TaskSourceChanged += OnTaskSourceChanged;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background, Tick, Application.Current.Dispatcher);
        }

        private void OnLoggedOn(object? sender, SteamUser.LoggedOnCallback e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(IsConnected));
                ConnectCommand.Toggle(true);
            });
        }

        private void OnConnect(object? obj)
        {
            ConnectCommand.Toggle(false);
            if (IsConnected)
                _steam.Disconnect();
            else
                _steam.Connect();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SimpleCommand CancelCommand { get; }

        public SimpleCommand ConnectCommand { get; }

        public string Description { get; private set; } = string.Empty;

        public bool IsConnected => _steam.User.SteamID?.IsAnonAccount ?? false;

        public double Progress { get; private set; }

        public bool IsTaskSet() => App.TaskBlocker.IsSet(SteamTask);

        public void ReleaseTask()
        {
            App.TaskBlocker.Release(SteamTask);
            Description = string.Empty;
            _cts = null;
            _timer.Stop();
            OnPropertyChanged(nameof(Description));
        }

        public void Report(double value)
        {
            lock (_progressLock)
            {
                _progress = Math.Clamp(value, 0, 100);
            }
        }

        public bool CanExecute(bool displayError = true)
        {
            if (!IsConnected)
            {
                if(displayError)
                    new ErrorModal("Steam Error", "You must be connected to Steam for this opperation.").ShowDialog();
                return false;
            }
            return true;
        }

        public CancellationTokenSource SetTask(string description)
        {
            if (App.TaskBlocker.IsSet(SteamTask))
                throw new InvalidOperationException("Steam task already set.");

            Description = description;
            OnPropertyChanged(nameof(Description));
            _timer.Start();
            return _cts = App.TaskBlocker.Set(SteamTask);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCancel(object? obj)
        {
            App.TaskBlocker.Cancel(SteamTask);
            Description = "Canceling...";
            OnPropertyChanged(nameof(Description));
        }

        private void OnConnected(object? sender, SteamClient.ConnectedCallback e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(IsConnected));
                ConnectCommand.Toggle(true);
            });
        }

        private void OnDisconnected(object? sender, SteamClient.DisconnectedCallback e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(IsConnected));
                ConnectCommand.Toggle(true);
            });
        }

        private void OnTaskSourceChanged(object? sender, string e)
        {
            if (e != SteamTask) return;

            if (!App.TaskBlocker.IsSet(SteamTask))
            {
                Description = string.Empty;
                _progress = 0;
            }
            else
            {
                _steam.ContentDownloader.SetProgress(this);
            }
        }


        private void Tick(object? sender, EventArgs e)
        {
            lock (_progressLock)
            {
                Progress = _progress;
                OnPropertyChanged(nameof(Progress));
            }
        }
    }
}