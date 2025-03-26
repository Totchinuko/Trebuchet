﻿using System;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet
{
    public class SteamWidget : IProgress<double>,
        INotifyPropertyChanged,
        IRecipient<SteamConnectionChangedMessage>,
        IRecipient<OperationStateChanged>
    {
        private readonly object _descriptionLock = new();
        private readonly object _progressLock;
        private string _description = string.Empty;
        private double _progress;
        private readonly DispatcherTimer _timer;

        public SteamWidget()
        {
            StrongReferenceMessenger.Default.RegisterAll(this);

            _progressLock = new object();
            CancelCommand = new SimpleCommand(OnCancel);
            ConnectCommand = new SimpleCommand(OnConnect);

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, Tick);
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
        
        public bool IsLoading => !string.IsNullOrEmpty(Description);

        public double Progress { get; private set; }
        
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

        public void Receive(SteamConnectionChangedMessage message)
        {
            Dispatcher.UIThread.Invoke(() =>
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
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(IsIndeterminate));
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

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCancel(object? obj)
        {
            StrongReferenceMessenger.Default.Send(new OperationCancelMessage(Operations.SteamDownload));
            SetDescription("Canceling...");
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsIndeterminate));
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
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }
    }
}