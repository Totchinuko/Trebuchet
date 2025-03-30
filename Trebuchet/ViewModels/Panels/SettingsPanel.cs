using System.IO;
using Microsoft.Extensions.Logging;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.Panels
{
    public class SettingsPanel : FieldEditorPanel
    {
        private readonly SteamAPI _steamApi;
        private readonly ILogger<SettingsPanel> _logger;
        private bool _displayedHelp;

        public SettingsPanel(AppSetup setup, UIConfig uiConfig, SteamAPI steamApi, ILogger<SettingsPanel> logger) : base(Resources.Settings, string.Empty, "mdi-cog", true)
        {
            _steamApi = steamApi;
            _logger = logger;
            AppSetup = setup;
            UiConfig = uiConfig;
            LoadPanel();
        }

        public AppSetup AppSetup { get; }

        public UIConfig UiConfig { get; }

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
            BuildFields("Trebuchet.ViewModels.Panels.SettingsPanel.Fields.json", AppSetup, nameof(AppSetup.Config));
            BuildFields("Trebuchet.ViewModels.Panels.SettingsPanel.UI.Fields.json", this, nameof(UiConfig));
        }

        protected override void OnValueChanged(string property)
        {
            AppSetup.Config.SaveFile();
            UiConfig.SaveFile();
            UpdateRequiredActions();
        }

        private async void DisplaySetupHelp()
        {
            if (_displayedHelp) return;
            _displayedHelp = true;

            if (AppSetup.Config.IsInstallPathValid && (Tools.IsClientInstallValid(AppSetup.Config) || Tools.IsServerInstallValid(AppSetup.Config)))
                return;

            if (!AppSetup.Config.IsInstallPathValid)
            {
                ErrorModal modal = new ErrorModal(
                    Resources.WelcomeInstallPathInvalid,
                    Resources.WelcomeInstallPathInvalidText);
                await modal.OpenDialogueAsync();
            }

            if ((!Tools.IsClientInstallValid(AppSetup.Config) && !Tools.IsServerInstallValid(AppSetup.Config)))
            {
                MessageModal modal = new MessageModal(
                  Resources.WelcomeSettingTutorial,
                  Resources.WelcomeSettingTutorialText,
                  250);
                await modal.OpenDialogueAsync();
            }
        }

        private void LoadPanel()
        {
            RefreshFields();
            UpdateRequiredActions();
        }

        private async void OnServerInstanceInstall(object? obj)
        {
            try
            {
                await _steamApi.UpdateServers();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal(Resources.Error, tex.Message).OpenDialogueAsync();
            }
        }

        private void UpdateRequiredActions()
        {
            RequiredActions.Clear();

            int installed = _steamApi.GetInstalledServerInstanceCount();
            if (AppSetup.Config.ServerInstanceCount > installed)
                RequiredActions.Add(
                    new RequiredCommand(
                        Resources.ServerNotInstalled, 
                        Resources.Install, 
                        OnServerInstanceInstall)
                        .SetBlockingType<SteamDownload>());
        }
    }
}