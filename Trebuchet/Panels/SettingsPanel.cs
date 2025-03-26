using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetLib;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.Panels
{
    public class SettingsPanel : FieldEditorPanel
    {
        private bool _displayedHelp;

        public SettingsPanel() : base("Settings", string.Empty, "mdi-cog", PanelPosition.Bottom)
        {
            LoadPanel();
        }

        public Config Config { get; } = StrongReferenceMessenger.Default.Send<ConfigRequest>();

        public UIConfig UiConfig => App.Config;

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
            BuildFields("Trebuchet.Panels.SettingsPanel.Fields.json", this, nameof(Config));
            BuildFields("Trebuchet.Panels.SettingsPanel.UI.Fields.json", this, nameof(UiConfig));
        }

        protected override void OnValueChanged(string property)
        {
            Config.SaveFile();
            App.Config.SaveFile();
            UpdateRequiredActions();
        }

        private async void DisplaySetupHelp()
        {
            if (_displayedHelp) return;
            _displayedHelp = true;

            if (Config.IsInstallPathValid && (Tools.IsClientInstallValid(Config) || Tools.IsServerInstallValid(Config)))
                return;

            if (!Config.IsInstallPathValid)
            {
                ErrorModal modal = new ErrorModal(
                    App.GetAppText("Welcome_InstallPathInvalid_Title"),
                    App.GetAppText("Welcome_InstallPathInvalid"));
                await modal.OpenDialogueAsync();
            }

            if ((!Tools.IsClientInstallValid(Config) && !Tools.IsServerInstallValid(Config)))
            {
                MessageModal modal = new MessageModal(
                  App.GetAppText("Welcome_SettingTutorial_Title"),
                  App.GetAppText("Welcome_SettingTutorial"),
                  250);
                await modal.OpenDialogueAsync();
            }
        }

        private void LoadPanel()
        {
            RefreshFields();
            UpdateRequiredActions();
        }

        private void OnAppRestart(object? obj)
        {
            Utils.Utils.RestartProcess(Config.IsTestLive);
        }

        private void OnServerInstanceInstall(object? obj)
        {
            StrongReferenceMessenger.Default.Send<ServerUpdateMessage>();
        }

        private void UpdateRequiredActions()
        {
            RequiredActions.Clear();

            int installed = StrongReferenceMessenger.Default.Send<InstanceInstalledCountRequest>();
            if (Directory.Exists(Config.ResolvedInstallPath) && Config.ServerInstanceCount > installed)
                RequiredActions.Add(new RequiredCommand(App.GetAppText("ServerNotInstalled"), App.GetAppText("Install"), OnServerInstanceInstall, Operations.SteamDownload));
        }
    }
}