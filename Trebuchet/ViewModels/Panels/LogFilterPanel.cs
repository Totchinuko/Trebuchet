using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    public class LogFilterPanel : ReactiveObject, IRefreshablePanel 
    {
        private bool _canBeOpened;
        private readonly AppSetup _setup;

        public LogFilterPanel(AppSetup setup)
        {
            _setup = setup;
            CanBeOpened = setup.Config is { ServerInstanceCount: > 0 };
        }

        public string Icon => @"mdi-filter";
        public string Label => Resources.PanelServerLogFilter;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public Task RefreshPanel()
        {
            CanBeOpened = _setup.Config is { ServerInstanceCount: > 0 };
            return Task.CompletedTask;
        }

    }
}