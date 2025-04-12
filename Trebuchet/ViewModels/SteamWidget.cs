using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels
{
    public class SteamWidget : ReactiveObject
    {
        private readonly Steam _steam;
        private string _description = string.Empty;
        private bool _isConnected;
        private readonly ObservableAsPropertyHelper<bool> _isIndeterminate;
        private double _progressBar;
        private bool _canConnect = true;
        private bool _isLoading;

        public SteamWidget(
            IProgressCallback<double> progress, 
            Steam steam, 
            TaskBlocker taskBlocker)
        {
            _steam = steam;
            progress.ProgressChanged += OnProgressChanged;

            _steam.Connected += OnSteamConnected;
            _steam.Disconnected += OnSteamDisconnected;

            CancelCommand = ReactiveCommand.Create(() =>
            {
                taskBlocker.Cancel<SteamDownload>();
                Description = @$"{Resources.Cancelling}...";
            });

            var canConnect = this.WhenAnyValue(x => x.CanConnect, x => x.IsConnected, (can, isc) => can && !isc);
            ConnectCommand = ReactiveCommand.CreateFromTask(_steam.Connect, canConnect);

            taskBlocker.TaskChanges
                .Where(x => x.EventArgs.Type.GetType() == typeof(SteamDownload))
                .Subscribe((pattern) =>
                {   
                    Progress = 0;
                    Description = pattern.EventArgs.Toggle ? pattern.EventArgs.Type.Label : string.Empty;
                    IsLoading = pattern.EventArgs.Toggle;
                });
                
            _isIndeterminate = this.WhenAnyValue(x => x.Progress)
                .Select(x => x == 0.0)
                .ToProperty(this, x => x.IsIndeterminate);
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

        public bool IsIndeterminate => _isIndeterminate.Value;

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