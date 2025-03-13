using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Panels;
using TrebuchetLib;

namespace Trebuchet.Panels
{
    public class LogFilterPanel : Panel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();

        public LogFilterPanel() : base("LogFilterPanel")
        {
            LoadPanel();
        }

        public override bool CanExecute(object? parameter)
        {
            return _config is { IsInstallPathValid: true, ServerInstanceCount: > 0 };
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