using System;
using System.Reactive.Linq;
using ReactiveUI;
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
            RefreshPanel.IsExecuting
                .Where(x => x)
                .Select(_ => _setup.Config is { ServerInstanceCount: > 0 })
                .Subscribe(x => CanTabBeClicked = x);
            RefreshPanel.Subscribe((_) =>
            {
                LoadPanel();
            });
            LoadPanel();
        }

        private void LoadPanel()
        {
        }
    }
}