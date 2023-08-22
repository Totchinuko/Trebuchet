using CommunityToolkit.Mvvm.Messaging;
using System.Windows;

namespace Trebuchet
{
    public class RconPanel : Panel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();

        public RconPanel()
        {
            LoadPanel();
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["RconPanel"];

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
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