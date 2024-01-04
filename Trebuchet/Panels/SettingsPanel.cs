using CommunityToolkit.Mvvm.Messaging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Trebuchet
{
    public class SettingsPanel : FieldEditorPanel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();

        public SettingsPanel()
        {
            LoadPanel();
        }

        public Config Config => _config;

        public UIConfig UIConfig => App.Config;

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
            if (Directory.Exists(_config.InstallPath) && _config.ServerInstanceCount > installed)
                RequiredActions.Add(new RequiredCommand("Some server instances are not yet installed.", "Install", OnServerInstanceInstall, Operations.SteamDownload));
            if (App.UseSoftwareRendering == App.Config.UseHardwareAcceleration)
                RequiredActions.Add(new RequiredCommand("Changing hardware acceleration require to restart the application", "Restart", OnAppRestart, Operations.SteamDownload));
        }
    }
}