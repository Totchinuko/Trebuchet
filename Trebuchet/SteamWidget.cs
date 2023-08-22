using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using SteamKit2;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Trebuchet
{
    public class SteamWidget : IProgress<double>,
        INotifyPropertyChanged,
        IRecipient<SteamConnectionChangedMessage>,
        IRecipient<OperationStateChanged>
    {
        private double _progress;
        private object _progressLock = new object();
        private DispatcherTimer _timer;

        public SteamWidget()
        {
            StrongReferenceMessenger.Default.RegisterAll(this);

            _progressLock = new object();
            CancelCommand = new SimpleCommand(OnCancel);
            ConnectCommand = new SimpleCommand(OnConnect);

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background, Tick, Application.Current.Dispatcher);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SimpleCommand CancelCommand { get; }

        public SimpleCommand ConnectCommand { get; }

        public string Description { get; private set; } = string.Empty;

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
            IsConnected = message.Value;
            OnPropertyChanged(nameof(IsConnected));
        }

        public void Receive(OperationStateChanged message)
        {
            if (message.key != Operations.SteamDownload) return;

            if (!message.Value)
            {
                Description = string.Empty;
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

        public void Start(string description)
        {
            Description = description;
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
            Description = "Canceling...";
            OnPropertyChanged(nameof(Description));
        }

        private void OnConnect(object? obj)
        {
            ConnectCommand.Toggle(false);
            if (!IsConnected)
                StrongReferenceMessenger.Default.Send<SteamConnectMessage>();
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