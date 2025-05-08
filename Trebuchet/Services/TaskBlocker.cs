using ReactiveUI;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public sealed class TaskBlocker : ReactiveObject
{
    private readonly Launcher _launcher;
    private readonly Steam _steam;
    private bool _canDownloadMods;
    private bool _canDownloadServers;
    private bool _canLaunch;
        

    public TaskBlocker(Launcher launcher, Steam steam)
    {
        _launcher = launcher;
        _steam = steam;
        RefreshStates();

        _launcher.StateChanged += (_, _) => RefreshStates();
        _steam.StatusChanged += (_, _) => RefreshStates();
    }

    public bool CanDownloadMods
    {
        get => _canDownloadMods;
        set => this.RaiseAndSetIfChanged(ref _canDownloadMods, value);
    }

    public bool CanDownloadServer
    {
        get => _canDownloadServers;
        set => this.RaiseAndSetIfChanged(ref _canDownloadServers, value);
    }

    public bool CanLaunch
    {
        get => _canLaunch;
        set => this.RaiseAndSetIfChanged(ref _canLaunch, value);
    }

    private void RefreshStates()
    {
        CanDownloadMods = _steam.Status == SteamStatus.StandBy
                          && !_launcher.IsAnyServerRunning()
                          && !_launcher.IsClientRunning();

        CanDownloadServer = _steam.Status == SteamStatus.StandBy
                            && !_launcher.IsAnyServerRunning();

        CanLaunch = _steam.Status == SteamStatus.StandBy;
    }
}