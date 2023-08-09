using Goog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class Settings : INotifyPropertyChanged, ITemplateHolder
    {
        private Config _config;
        private List<IField> _fields = new List<IField>();
        private List<RequiredAction> _requiredActions = new List<RequiredAction>();
        private CancellationTokenSource? _source;
        private WaitModal? _wait;

        public Settings(Config config)
        {
            _config = config;

            _fields = new List<IField>
            {
                new Field<string>("Install path", "InstallPath", _config.InstallPath, "DirectoryField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x?.Equals(string.Empty)??true, () => string.Empty),
                new Field<string>("Client path", "ClientPath", _config.ClientPath, "DirectoryField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x?.Equals(string.Empty)??true, () => string.Empty),
                new SliderField<double>("Server Instances", "ServerInstanceCount", _config.ServerInstanceCount, "SliderField")
                    .WithMinMax(0, 6)
                    .WithIntFrequency()
                    .WhenChanged(OnInstanceCountChanged)
                    .WithDefault((x) => x == 0, () => 0) ,
                new Field<string>("Steam API Key", "SteamAPIKey", _config.SteamAPIKey, "TextboxField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x?.Equals(string.Empty)??true, () => string.Empty),
                new Field<bool>("Display Steam CMD", "DisplayCMD", _config.DisplayCMD, "ToggleField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x == false, () => false),
            };

            UpdateRequiredActions();
        }

        public event EventHandler? ConfigChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<IField> Fields { get => _fields; set => _fields = value; }

        public List<RequiredAction> RequiredActions { get => _requiredActions; set => _requiredActions = value; }

        public DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];

        protected virtual void OnConfigChanged()
        {
            UpdateRequiredActions();
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CleanupTask(Task<int> task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _wait?.Close();
                _wait = null;
            });

            if (task.IsFaulted && task.Exception != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    new ExceptionModal(task.Exception).ShowDialog();
                });
                return;
            }

            if (task.Result != 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    new ErrorModal("Steam CMD", "Steam CMD terminated with an error code.", false).ShowDialog();
                });
                return;
            }
        }

        private void OnInstallSteam(object? obj)
        {
            if (_source != null) return;

            _source = new CancellationTokenSource();
            Task.Run(() => Setup.SetupAppAndSteam(_config, _source.Token, true)).ContinueWith(OnInstallSteamComplete);
            _wait = new WaitModal("Steam", "Installing Steam CMD...", TaskCancel);
            _wait.ShowDialog();
        }

        private void OnInstallSteamComplete(Task<int> task)
        {
            CleanupTask(task);

            if (_source != null && _source.IsCancellationRequested)
            {
                Setup.DeleteSteamCMD(_config);
            }

            _source?.Dispose();
            _source = null;
            UpdateRequiredActions();
        }

        private void OnInstanceCountChanged(string name, object? value)
        {
            if (value is not double n)
                throw new Exception($"{name} is not a double");
            OnValueChanged(name, (int)n);
        }

        private void OnServerInstanceInstall(object? obj)
        {
            if (_config.ServerInstanceCount <= 0)
                return;

            int installed = _config.GetInstalledInstances();
            if (installed >= _config.ServerInstanceCount)
                return;

            _source = new CancellationTokenSource();
            Task.Run(() => OnServerInstanceInstallTask(_source.Token)).ContinueWith(OnServerInstanceInstallComplete);
            _wait = new WaitModal("Server", "Updating...", TaskCancel);
            _wait.ShowDialog();
        }

        private void OnServerInstanceInstallComplete(Task<int> task)
        {
            CleanupTask(task);
            _source?.Dispose();
            _source = null;
            UpdateRequiredActions();
        }

        private async Task<int> OnServerInstanceInstallTask(CancellationToken token)
        {
            int count = _config.ServerInstanceCount;
            for (int i = 0; i < count; i++)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if(_wait != null)
                        _wait.Message = $"Updating server instance {i}...";
                });
                int code = await Setup.UpdateServer(_config, i, token, false);
                if (code != 0)
                    return code;
            }
            return 0;
        }

        private void OnValueChanged(string name, object? value)
        {
            PropertyInfo? property = _config.GetType().GetProperty(name);
            if (property == null)
                throw new Exception($"Could not find property {name}");

            property.SetValue(_config, value);
            _config.SaveFile();
            OnConfigChanged();
        }

        private void TaskCancel()
        {
            _source?.Cancel();
        }

        private void UpdateRequiredActions()
        {
            _requiredActions = new List<RequiredAction>();

            int installed = _config.GetInstalledInstances();
            if (!File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)))
                _requiredActions.Add(new RequiredAction("Steam CMD is not yet installed.", "Install", OnInstallSteam));
            else if (_config.ServerInstanceCount > installed)
                _requiredActions.Add(new RequiredAction("Some server instances are not yet installed.", "Install", OnServerInstanceInstall));
            OnPropertyChanged("RequiredActions");
        }
    }
}