using System.IO;

using CommunityToolkit.Mvvm.Messaging;
using TrebuchetGUILib;

namespace Trebuchet
{
    public class SettingsPanel : FieldEditorPanel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private bool _displayedHelp = false;

        public SettingsPanel()
        {
            LoadPanel();
        }

        public Config Config => _config;

        public UIConfig UIConfig => App.Config;

        public override void OnWindowShow()
        {
            DisplaySetupHelp();
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("Trebuchet.Panels.SettingsPanel.Fields.json", this, "Config");
            BuildFields("Trebuchet.Panels.SettingsPanel.UI.Fields.json", this, "UIConfig");
        }

        protected override void OnValueChanged(string property)
        {
            _config.SaveFile();
            App.Config.SaveFile();
            UpdateRequiredActions();
        }

        private void DisplaySetupHelp()
        {
            if (_displayedHelp) return;
            _displayedHelp = true;

            if (_config.IsInstallPathValid && (Tools.IsClientInstallValid(_config) || Tools.IsServerInstallValid(_config)))
                return;

            if (!_config.IsInstallPathValid)
            {
                ErrorModal modal = new ErrorModal(
                    App.GetAppText("Welcome_InstallPathInvalid_Title"),
                    App.GetAppText("Welcome_InstallPathInvalid"));
                modal.ShowDialog();
            }

            if ((!Tools.IsClientInstallValid(_config) && !Tools.IsServerInstallValid(_config)))
            {
                MessageModal modal = new MessageModal(
                  App.GetAppText("Welcome_SettingTutorial_Title"),
                  App.GetAppText("Welcome_SettingTutorial"),
                  250);
                modal.ShowDialog();
            }
        }

        private void LoadPanel()
        {
            RefreshFields();
            UpdateRequiredActions();
        }

        private void OnAppRestart(object? obj)
        {
            GuiExtensions.RestartProcess(_config.IsTestLive, false);
        }

        private void OnServerInstanceInstall(object? obj)
        {
            StrongReferenceMessenger.Default.Send<ServerUpdateMessage>();
        }

        private void UpdateRequiredActions()
        {
            RequiredActions.Clear();

            int installed = StrongReferenceMessenger.Default.Send<InstanceInstalledCountRequest>();
            if (Directory.Exists(_config.ResolvedInstallPath) && _config.ServerInstanceCount > installed)
                RequiredActions.Add(new RequiredCommand("Some server instances are not yet installed.", "Install", OnServerInstanceInstall, Operations.SteamDownload));
        }
    }
}