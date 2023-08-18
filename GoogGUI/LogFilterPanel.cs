using Goog;
using System.IO;

namespace GoogGUI
{
    [Panel("Log Filter", "/Icons/Filter.png", false, 400, "LogFilterPanel", "Server")]
    public class LogFilterPanel : Panel
    {
        public LogFilterPanel(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            LoadPanel();
        }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                   File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                   _config.ServerInstanceCount > 0;
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        private void LoadPanel()
        {
        }
    }
}