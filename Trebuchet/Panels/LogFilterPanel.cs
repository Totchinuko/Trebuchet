using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Panels;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Panels
{
    public class LogFilterPanel : Panel
    {
        private readonly AppSetup _setup;

        public LogFilterPanel(AppSetup setup) : base("Log Filter", "LogFilterPanel", "mdi-filter", false)
        {
            _setup = setup;
            LoadPanel();
        }

        public override bool CanExecute(object? parameter)
        {
            return _setup.Config is { IsInstallPathValid: true, ServerInstanceCount: > 0 };
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