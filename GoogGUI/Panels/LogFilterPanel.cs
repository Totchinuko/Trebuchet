using Goog;
using System.IO;
using System.Windows;

namespace GoogGUI
{
    public class LogFilterPanel : Panel
    {
        public LogFilterPanel(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            LoadPanel();
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["LogFilterPanel"];

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