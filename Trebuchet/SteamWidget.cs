using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetGUILib;

namespace Trebuchet
{
    public class SteamWidget : IProgress<double>,
        INotifyPropertyChanged,
        IRecipient<SteamConnectionChangedMessage>,
        IRecipient<OperationStateChanged>
    {
        private readonly object _descriptionLock = new object();
        private readonly object _progressLock = new object();
        private string _description = string.Empty;
        private double _progress;
        private DispatcherTimer _timer;

        public SteamWidget()
        {
            StrongReferenceMessenger.Default.RegisterAll(this);

            _progressLock = new object();
            CancelCommand = new SimpleCommand(OnCancel);
            ConnectCommand = new SimpleCommand(OnConnect);

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, Tick, Application.Current.Dispatcher);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SimpleCommand CancelCommand { get; }

        public SimpleCommand ConnectCommand { get; }

        public string Description
        {
            get
            {
                lock (_descriptionLock)
                    return _description;
            }
        }

        public bool IsConnected { get; private set; }

        public double Progress { get; private set; }

        public bool CanExecute(bool displayError = true)
        {
            if (!IsConnected)
            {
                if (displayError)
                    new ErrorModal("Steam Error", "You must be connected to Steam for this opperation.").ShowDialog();
                return false;
            }
            return true;
        }

        public void Receive(SteamConnectionChangedMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                IsConnected = message.Value;
                OnPropertyChanged(nameof(IsConnected));
                ConnectCommand.Toggle(true);
            });
        }

        public void Receive(OperationStateChanged message)
        {
            if (message.key != Operations.SteamDownload) return;

            if (!message.Value)
            {
                SetDescription(string.Empty);
                _timer.Stop();
                _progress = 0;
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(Progress));
            }
        }

        public void Report(double value)
        {
            lock (_progressLock)
            {
                _progress = Math.Clamp(value, 0, 100);
            }
        }

        public void SetDescription(string description)
        {
            lock (_descriptionLock)
            {
                _description = description;
                OnPropertyChanged(nameof(Description));
            }
        }

        public void Start(string description)
        {
            SetDescription(description);
            OnPropertyChanged(nameof(Description));
            _timer.Start();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCancel(object? obj)
        {
            StrongReferenceMessenger.Default.Send(new OperationCancelMessage(Operations.SteamDownload));
            SetDescription("Canceling...");
            OnPropertyChanged(nameof(Description));
        }

        private void OnConnect(object? obj)
        {
            if (!IsConnected)
            {
                ConnectCommand.Toggle(false);
                StrongReferenceMessenger.Default.Send<SteamConnectMessage>();
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