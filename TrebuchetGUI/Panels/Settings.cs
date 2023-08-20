using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Trebuchet
{
    public class Settings : FieldEditorPanel
    {
        private readonly SteamSession _steam;
        private readonly SteamWidget _steamWidget;

        public Settings(Config config, UIConfig uiConfig, SteamSession steam, SteamWidget steamWidget) : base(config, uiConfig)
        {
            _steam = steam;
            _steamWidget = steamWidget;
            LoadPanel();
        }

        public Config Config => _config;

        public UIConfig UIConfig => _uiConfig;

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("TrebuchetGUI.Panels.Settings.Fields.json", this, "Config");
            BuildFields("TrebuchetGUI.Panels.Settings.UI.Fields.json", this, "UIConfig");
        }

        protected override void OnValueChanged(string property)
        {
            _config.SaveFile();
            UpdateRequiredActions();
        }

        private void HandleTaskErrors(Task<int> task)
        {
            if (task.IsFaulted && task.Exception != null)
                new ExceptionModal(task.Exception).ShowDialog();
            else if (task.Result != 0)
                new ErrorModal("Steam CMD", "Steam CMD terminated with an error code.", false).ShowDialog();
        }

        private void LoadPanel()
        {
            RefreshFields();
            UpdateRequiredActions();
        }

        private void OnAppRestart(object? obj)
        {
            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }

        private void OnServerInstanceInstall(object? obj)
        {
            if (_config.ServerInstanceCount <= 0) return;
            if (!_steamWidget.CanExecute()) return;
            if (App.TaskBlocker.IsSet(Dashboard.GameTask)) return;

            int installed = Setup.GetInstalledInstances(_config);
            if (installed >= _config.ServerInstanceCount)
                return;

            var cts = _steamWidget.SetTask("Updating server instances...");

            Task.Run(async () =>
            {
                try
                {
                    await Setup.UpdateServerInstances(_config, _steam, cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Mod update failed", $"Mod update failed. Please check the log for more information. ({ex.Message})").ShowDialog();
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _steamWidget.ReleaseTask();
                        UpdateRequiredActions();
                    });
                }
            }, cts.Token);
        }

        private void UpdateRequiredActions()
        {
            RequiredActions.Clear();

            int installed = Setup.GetInstalledInstances(_config);
            if (Directory.Exists(_config.InstallPath) && _config.ServerInstanceCount > installed)
                RequiredActions.Add(new RequiredCommand("Some server instances are not yet installed.", "Install", OnServerInstanceInstall, SteamWidget.SteamTask));
            if (App.UseSoftwareRendering == _uiConfig.UseHardwareAcceleration)
                RequiredActions.Add(new RequiredCommand("Changing hardware acceleration require to restart the application", "Restart", OnAppRestart, SteamWidget.SteamTask));
        }
    }
}