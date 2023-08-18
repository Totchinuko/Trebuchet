using Goog;
using System.IO;

namespace GoogGUI
{
    [Panel("Rcon", "/Icons/Steam.png", false, 300, "RconPanel", "Server")]
    public class RconPanel : Panel
    {
        public RconPanel(Config config, UIConfig uiConfig) : base(config, uiConfig)
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