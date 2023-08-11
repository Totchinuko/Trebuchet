using Goog;
using GoogGUI.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogGUI
{
    [Panel("Settings", "/Icons/Settings.png", true, 10)]
    public class Settings : FieldEditorPanel
    {
        public Settings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            _config.FileSaved += OnConfigSaved;
            BuildFields();
            UpdateRequiredActions();
        }

        #region Fields
        [DirectoryField("Install Path", Sort = 0)]
        public string InstallPath
        {
            get => _config.InstallPath;
            set
            {
                _config.InstallPath = value;
                OnValueChanged();
            }
        }

        [DirectoryField("Game Path", Sort = 10)]
        public string ClientPath
        {
            get => _config.ClientPath;
            set
            {
                _config.ClientPath = value;
                OnValueChanged();
            }
        }

        [IntSliderField("Server Instances", 0, 6, Frequency = 1, Sort = 20)]
        public int ServerInstanceCount
        {
            get => _config.ServerInstanceCount;
            set
            {
                _config.ServerInstanceCount = value;
                OnValueChanged();
            }
        }

        [ToggleField("Display Steam CMD", Sort = 30)]
        public bool DisplayCMD
        {
            get => _config.DisplayCMD;
            set
            {
                _config.DisplayCMD = value;
                OnValueChanged();
            }
        }

        [ToggleField("Use Hardware Acceleration", defaultValue: true, Sort = 40)]
        public bool UseHardwareAcceleration
        {
            get => _uiConfig.UseHardwareAcceleration;
            set
            {
                _uiConfig.UseHardwareAcceleration = value;
                OnValueChanged();
            }
        }
        #endregion

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

        private void OnConfigSaved(object? sender, Config e)
        {
            OnCanExecuteChanged();
        }

        private void OnInstallSteam(object? obj)
        {
            if (!App.TaskBlocker.IsAvailable) return;

            var token = App.TaskBlocker.SetMain("Installing steam CMD...");
            var task = Task.Run(() => Setup.SetupAppAndSteam(_config, token, true), token).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnInstallSteamComplete(x)));
        }

        private void OnInstallSteamComplete(Task<int> task)
        {
            HandleTaskErrors(task);
            if (task.IsCanceled)
                Setup.DeleteSteamCMD(_config);

            App.TaskBlocker.ReleaseMain();
            UpdateRequiredActions();
        }

        private void OnServerInstanceInstall(object? obj)
        {
            if (_config.ServerInstanceCount <= 0) return;
            if (!App.TaskBlocker.IsAvailable) return;

            int installed = _config.GetInstalledInstances();
            if (installed >= _config.ServerInstanceCount)
                return;

            var token = App.TaskBlocker.SetMain("Updating server instances...");
            var task = Task.Run(() => OnServerInstanceInstallTask(token), token).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnServerInstanceInstallComplete(x)));
        }

        private void OnServerInstanceInstallComplete(Task<int> task)
        {
            HandleTaskErrors(task);
            App.TaskBlocker.ReleaseMain();
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

        private void OnValueChanged()
        {
            _config.SaveFile();
            UpdateRequiredActions();
        }

        private void UpdateRequiredActions()
        {
            RequiredActions.Clear();

            int installed = _config.GetInstalledInstances();
            if (Directory.Exists(_config.InstallPath) && !File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)))
                RequiredActions.Add(new RequiredCommand("Steam CMD is not yet installed.", "Install", OnInstallSteam, true));
            else if (Directory.Exists(_config.InstallPath) && _config.ServerInstanceCount > installed)
                RequiredActions.Add(new RequiredCommand("Some server instances are not yet installed.", "Install", OnServerInstanceInstall, true));
            if (App.UseSoftwareRendering == _uiConfig.UseHardwareAcceleration)
                RequiredActions.Add(new RequiredCommand("Changing hardware acceleration require to restart the application", "Restart", OnAppRestart, true));
        }
    }
}