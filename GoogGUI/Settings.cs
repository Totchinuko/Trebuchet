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
        private List<RequiredCommand> _requiredActions = new List<RequiredCommand>();
        private CancellationTokenSource? _source;

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
                new Field<bool>("Display Steam CMD", "DisplayCMD", _config.DisplayCMD, "ToggleField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x == false, () => false),
                new Field<bool>("Use Hardware Acceleration", "UseHardwareAcceleration", _config.UseHardwareAcceleration, "ToggleField")
                    .WhenChanged(OnValueChanged)
                    .WithDefault((x) => x == true, () => true),
            };

            UpdateRequiredActions();
        }

        public event EventHandler? ConfigChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<IField> Fields { get => _fields; set => _fields = value; }

        public List<RequiredCommand> RequiredActions { get => _requiredActions; set => _requiredActions = value; }

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

        private void HandleTaskErrors(Task<int> task)
        {
            if (task.IsFaulted && task.Exception != null)
                new ExceptionModal(task.Exception).ShowDialog();
            else if (task.Result != 0)
                new ErrorModal("Steam CMD", "Steam CMD terminated with an error code.", false).ShowDialog();
        }

        private void OnAppRestart(object? obj)
        {
            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }

        private void OnInstallSteam(object? obj)
        {
            if (_source != null) return;
            if (!App.TaskBlocker.IsAvailable) return;

            _source = new CancellationTokenSource();
            var task = Task.Run(() => Setup.SetupAppAndSteam(_config, _source.Token, true)).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnInstallSteamComplete(x)));
            App.TaskBlocker.Set(task, "Installing steam CMD...", _source);
        }

        private void OnInstallSteamComplete(Task<int> task)
        {
            HandleTaskErrors(task);
            if (_source != null && _source.IsCancellationRequested)
                Setup.DeleteSteamCMD(_config);

            _source?.Dispose();
            _source = null;
            App.TaskBlocker.Release();
            UpdateRequiredActions();
            OnConfigChanged();
        }

        private void OnInstanceCountChanged(string name, object? value)
        {
            if (value is not double n)
                throw new Exception($"{name} is not a double");
            OnValueChanged(name, (int)n);
        }

        private void OnServerInstanceInstall(object? obj)
        {
            if (_config.ServerInstanceCount <= 0) return;
            if (_source != null) return;
            if (!App.TaskBlocker.IsAvailable) return;

            int installed = _config.GetInstalledInstances();
            if (installed >= _config.ServerInstanceCount)
                return;

            _source = new CancellationTokenSource();
            var task = Task.Run(() => OnServerInstanceInstallTask(_source.Token)).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnServerInstanceInstallComplete(x)));
            App.TaskBlocker.Set(task, "Updating server instances...", _source);
        }

        private void OnServerInstanceInstallComplete(Task<int> task)
        {
            HandleTaskErrors(task);
            _source?.Dispose();
            _source = null;
            App.TaskBlocker.Release();
            UpdateRequiredActions();
        }

        private async Task<int> OnServerInstanceInstallTask(CancellationToken token)
        {
            int count = _config.ServerInstanceCount;
            for (int i = 0; i < count; i++)
            {
                Application.Current.Dispatcher.Invoke(() => App.TaskBlocker.Description = $"Updating server instance {i}...");
                if (i == 0)
                {
                    int code = await Setup.UpdateServer(_config, i, token, false);
                    if (code != 0)
                        return code;
                }
                else
                {
                    await Setup.UpdateServerFromInstance0(_config, i, token);
                }
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

        private void UpdateRequiredActions()
        {
            _requiredActions = new List<RequiredCommand>();

            int installed = _config.GetInstalledInstances();
            if (!File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)))
                _requiredActions.Add(new RequiredCommand("Steam CMD is not yet installed.", "Install", OnInstallSteam, true));
            else if (_config.ServerInstanceCount > installed)
                _requiredActions.Add(new RequiredCommand("Some server instances are not yet installed.", "Install", OnServerInstanceInstall, true));
            if (App.UseSoftwareRendering == _config.UseHardwareAcceleration)
                _requiredActions.Add(new RequiredCommand("Changing hardware acceleration require to restart the application", "Restart", OnAppRestart, true));
            OnPropertyChanged("RequiredActions");
        }
    }
}