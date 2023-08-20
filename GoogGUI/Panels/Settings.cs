using Goog;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class Settings : FieldEditorPanel
    {
        public Settings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
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
            BuildFields("GoogGUI.Panels.Settings.Fields.json", this, "Config");
            BuildFields("GoogGUI.Panels.Settings.UI.Fields.json", this, "UIConfig");
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
            if (!App.TaskBlocker.IsAvailable) return;

            int installed = Setup.GetInstalledInstances(_config);
            if (installed >= _config.ServerInstanceCount)
                return;

            App.GetApp().GetPanel<Dashboard>().UpdateServer();
        }

        private void UpdateRequiredActions()
        {
            RequiredActions.Clear();

            int installed = Setup.GetInstalledInstances(_config);
            if (Directory.Exists(_config.InstallPath) && _config.ServerInstanceCount > installed)
                RequiredActions.Add(new RequiredCommand("Some server instances are not yet installed.", "Install", OnServerInstanceInstall, TaskBlocker.MainTask));
            if (App.UseSoftwareRendering == _uiConfig.UseHardwareAcceleration)
                RequiredActions.Add(new RequiredCommand("Changing hardware acceleration require to restart the application", "Restart", OnAppRestart, TaskBlocker.MainTask));
        }
    }
}