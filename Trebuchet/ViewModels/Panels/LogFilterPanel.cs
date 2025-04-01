using Trebuchet.Assets;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    public class LogFilterPanel : Panel
    {
        private readonly AppSetup _setup;

        public LogFilterPanel(AppSetup setup) : base(Resources.ServerLogFilter, "mdi-filter", false)
        {
            _setup = setup;
            LoadPanel();
        }

        public override bool CanExecute(object? parameter)
        {
            return _setup.Config is { ServerInstanceCount: > 0 };
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