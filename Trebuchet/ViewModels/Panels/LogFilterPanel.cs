using System.Threading.Tasks;
using Trebuchet.Assets;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    public class LogFilterPanel(AppSetup setup) : Panel(Resources.PanelServerLogFilter, "mdi-filter", false)
    {
        public override Task RefreshPanel()
        {
            CanTabBeClicked = setup.Config is { ServerInstanceCount: > 0 };
            return Task.CompletedTask;
        }
    }
}