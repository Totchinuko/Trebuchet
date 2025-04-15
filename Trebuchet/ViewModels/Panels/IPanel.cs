using System;
using System.Threading.Tasks;
using ReactiveUI;
using tot_lib;

namespace Trebuchet.ViewModels.Panels;

public interface IPanel : IReactiveObject
{
    string Icon { get; }
    string Label { get; }
    bool CanBeOpened { get; }
}

public interface IBottomPanel : IPanel
{
}

public interface ITickingPanel : IPanel
{
    Task TickPanel();
}

public interface IRefreshablePanel : IPanel
{
    Task RefreshPanel();
}

public interface IRefreshingPanel : IPanel
{
    event AsyncEventHandler? RequestRefresh;
}

public interface IDisplablePanel : IPanel
{
    Task DisplayPanel();
}