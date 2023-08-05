using Goog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;

namespace GoogGUI
{
    public class SetupModal : BaseModal, INotifyPropertyChanged
    {
        private Action _callback;
        private Config _config;
        private bool _reinstall;
        private string _setupMessage = string.Empty;
        private CancellationTokenSource _source;

        public SetupModal(Config config, bool reinstall, Action callback)
        {
            CancelCommand = new SimpleCommand(OnCancel);
            _source = new CancellationTokenSource();
            _config = config;
            _reinstall = reinstall;
            _callback = callback;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand CancelCommand { get; private set; }

        public override int ModalHeight => 450;

        public override string ModalTitle => "Goog Setup";

        public override int ModalWidth => 650;

        public string SetupMessage { get => _setupMessage; set => _setupMessage = value; }
        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["Setup"];

        public virtual void CancelSetup()
        {
            _source.Cancel();
        }

        public override void OnWindowClose()
        {
            CancelSetup();
            _source.Dispose();
        }

        public void StartSetup()
        {
            _source.CancelAfter(30 * 1000);
            Task.Run(() => RunSetup(_config, _reinstall, _source.Token)).ContinueWith(OnSetupCompleted);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual async Task RunSetup(Config config, bool reinstall, CancellationToken token)
        {
            _setupMessage = "Downloading Steam CMD...";
            OnPropertyChanged("SetupMessage");
            await Setup.SetupApp(config, token, reinstall);
            if (config.ManageServers && !token.IsCancellationRequested)
            {
                _setupMessage = "Installing server...";
                OnPropertyChanged("SetupMessage");
                int result = await Setup.UpdateServer(config, token, reinstall);
                if (result != 0)
                    throw new Exception("Failed to update the server binaries.");
            }
        }

        private void OnCancel(object? obj)
        {
            CancelSetup();
        }

        private void OnSetupCompleted(Task task)
        {
            if (_source.IsCancellationRequested) return;
            _window.Close();
            _callback?.Invoke();
            new MessageModal("Success", "Goog necessary components were installed successfuly.").ShowDialog();
        }
    }
}