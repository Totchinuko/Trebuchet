using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Trebuchet.Messages;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;
using TrebuchetUtils.Modals;
using AppFiles = TrebuchetLib.Services.AppFiles;

namespace Trebuchet;

public class ClientInstanceDashboard : INotifyPropertyChanged
{
    private readonly Config _config;
    private readonly UIConfig _uiConfig;
    private readonly ILogger _logger;
    private readonly AppFiles _appFiles;
    private readonly SteamAPI _steamApi;
    private ProcessState _lastState;
    private string _selectedModlist;
    private string _selectedProfile;
    private readonly Launcher _launcher;

    public ClientInstanceDashboard(
        Config config, 
        UIConfig uiConfig,
        AppFiles appFiles,
        SteamAPI steamApi,
        Launcher launcher,
        ILogger logger)
    {
        _config = config;
        _uiConfig = uiConfig;
        _appFiles = appFiles;
        _steamApi = steamApi;
        _logger = logger;
        _launcher = launcher;

        KillCommand = new SimpleCommand(OnKilled, false);
        LaunchCommand = new TaskBlockedCommand(OnLaunched)
            .SetBlockingType<SteamDownload>();
        LaunchBattleEyeCommand = new TaskBlockedCommand(OnBattleEyeLaunched)
            .SetBlockingType<SteamDownload>();
        UpdateModsCommand = new TaskBlockedCommand(OnModUpdate)
            .SetBlockingType<SteamDownload>()
            .SetBlockingType<ClientRunning>()
            .SetBlockingType<ServersRunning>();

        _selectedProfile = _uiConfig.DashboardClientProfile;
        _selectedModlist = _uiConfig.DashboardClientModlist;

        Resolve();
        ListProfiles();
    }

    public bool CanUseDashboard => _config.IsInstallPathValid &&
                                   !string.IsNullOrEmpty(_config.ClientPath) &&
                                   File.Exists(Path.Combine(_config.ClientPath, Constants.FolderGameBinaries,
                                       Constants.FileClientBin));

    public bool IsUpdateNeeded => UpdateNeeded.Count > 0;

    public SimpleCommand KillCommand { get; }

    public TaskBlockedCommand LaunchBattleEyeCommand { get; }

    public TaskBlockedCommand LaunchCommand { get; }

    public List<string> Modlists { get; private set; } = new();

    public bool ProcessRunning { get; private set; }

    public IProcessStats ProcessStats { get; } = new ProcessStatsLight();

    public List<string> Profiles { get; private set; } = new();

    public string SelectedModlist
    {
        get => _selectedModlist;
        set
        {
            _selectedModlist = value;
            _uiConfig.DashboardClientModlist = _selectedModlist;
            _uiConfig.SaveFile();
        }
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            _selectedProfile = value;
            _uiConfig.DashboardClientProfile = _selectedProfile;
            _uiConfig.SaveFile();
        }
    }

    public TaskBlockedCommand UpdateModsCommand { get; private set; }

    public List<ulong> UpdateNeeded { get; private set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public async void Kill()
    {
        if (!ProcessRunning) return;

        if (_uiConfig.DisplayWarningOnKill)
        {
            var question = new QuestionModal(App.GetAppText("Kill_Title"), App.GetAppText("Kill_Message"));
            await question.OpenDialogueAsync();
            if (!question.Result) return;
        }

        KillCommand.Toggle(false);
        StrongReferenceMessenger.Default.Send(new KillProcessMessage(-1));
    }

    public async Task Launch(bool isBattleEye)
    {
        if (ProcessRunning) return;

        LaunchCommand.Toggle(false);
        LaunchBattleEyeCommand.Toggle(false);

        if (_config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning())
        {
            var modlist = _appFiles.Mods.CollectAllMods(SelectedModlist).ToList();
            await _steamApi.UpdateMods(modlist);
        }
        
        await _launcher.CatapultClient(SelectedProfile, SelectedModlist, isBattleEye);
    }

    public void RefreshSelection()
    {
        Resolve();
        ListProfiles();
    }

    public void ProcessRefresh(IConanProcess? process)
    {
        var state = process?.State ?? ProcessState.STOPPED;
        if (_lastState.IsRunning() && !state.IsRunning())
            OnProcessTerminated();
        else if (!_lastState.IsRunning() && state.IsRunning() && process is not null)
            OnProcessStarted(process);

        if (state == ProcessState.FAILED)
            OnProcessFailed();
        else if (ProcessRunning && process is not null)
            ProcessStats.SetDetails(process);

        _lastState = state;
    }

    protected virtual void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    //todo: Need to update somewhere
    private async Task CheckModUpdate()
    {
        ClearUpdates();
        if (string.IsNullOrEmpty(SelectedModlist)) return;
        var mods = _appFiles.Mods.CollectAllMods(SelectedModlist).ToList();
        var response = await _steamApi.RequestModDetails(mods);
        UpdateNeeded = _steamApi.CheckModsForUpdate(response.GetManifestKeyValuePairs().ToList());
        OnPropertyChanged(nameof(UpdateNeeded));
        OnPropertyChanged(nameof(IsUpdateNeeded));
    }

    private void ClearUpdates()
    {
        UpdateNeeded.Clear();
        OnPropertyChanged(nameof(UpdateNeeded));
        OnPropertyChanged(nameof(IsUpdateNeeded));
    }

    private void ListProfiles()
    {
        Modlists = _appFiles.Client.ListProfiles().ToList();
        Profiles = _appFiles.Client.ListProfiles().ToList();
        OnPropertyChanged(nameof(Modlists));
        OnPropertyChanged(nameof(Profiles));
    }

    private async void OnBattleEyeLaunched(object? obj)
    {
        await Launch(true);
    }

    private void OnKilled(object? obj)
    {
        Kill();
    }

    private async void OnLaunched(object? obj)
    {
        await Launch(false);
    }

    private async void OnModUpdate(object? obj)
    {
        if (string.IsNullOrEmpty(SelectedModlist)) return;

        var modlist = _appFiles.Mods.CollectAllMods(SelectedModlist).ToList();
        await _steamApi.UpdateMods(modlist);
    }

    private async void OnProcessFailed()
    {
        KillCommand.Toggle(false);
        LaunchCommand.Toggle(true);
        LaunchBattleEyeCommand.Toggle(true);
        await new ErrorModal("Client failed to start", "See the logs for more information.").OpenDialogueAsync();
    }

    private void OnProcessStarted(IConanProcess details)
    {
        LaunchCommand.Toggle(false);
        LaunchBattleEyeCommand.Toggle(false);
        KillCommand.Toggle(true);

        ProcessRunning = true;
        OnPropertyChanged(nameof(ProcessRunning));

        ProcessStats.StartStats(details);
        StrongReferenceMessenger.Default.Send<DashboardStateChanged>();
    }

    private void OnProcessTerminated()
    {
        ProcessStats.StopStats();
        KillCommand.Toggle(false);
        LaunchCommand.Toggle(true);
        LaunchBattleEyeCommand.Toggle(true);

        ProcessRunning = false;
        OnPropertyChanged(nameof(ProcessRunning));
        StrongReferenceMessenger.Default.Send<DashboardStateChanged>();
    }

    private void Resolve()
    {
        _appFiles.Client.ResolveProfile(ref _selectedProfile);
        _appFiles.Mods.ResolveProfile(ref _selectedModlist);

        _uiConfig.DashboardClientModlist = _selectedModlist;
        _uiConfig.DashboardClientProfile = _selectedProfile;
        _uiConfig.SaveFile();
        OnPropertyChanged(nameof(SelectedModlist));
        OnPropertyChanged(nameof(SelectedProfile));
    }
}