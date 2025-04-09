using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.SettingFields;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels;

public class SettingsPanel : Panel
{
    private readonly AppSetup _setup;
    private readonly UIConfig _uiConfig;
    private readonly SteamAPI _steamApi;
    private readonly OnBoarding _onBoarding;
    private readonly DialogueBox _box;
    private readonly ILogger<SettingsPanel> _logger;

    public SettingsPanel(
        AppSetup setup, 
        UIConfig uiConfig, 
        SteamAPI steamApi, 
        OnBoarding onBoarding,
        DialogueBox box,
        ILogger<SettingsPanel> logger) : 
        base(Resources.Settings, "mdi-cog", true)
    {
        _setup = setup;
        _uiConfig = uiConfig;
        _steamApi = steamApi;
        _onBoarding = onBoarding;
        _box = box;
        _logger = logger;

        SaveConfig = ReactiveCommand.Create(() => _setup.Config.SaveFile());
        SaveUiConfig = ReactiveCommand.Create(() => _uiConfig.SaveFile());
        RemoveUnusedMods = ReactiveCommand.CreateFromTask(OnRemoveUnusedMods);
            
        BuildFields();
    }

    public ReactiveCommand<Unit,Unit> SaveConfig { get; }
    public ReactiveCommand<Unit,Unit> SaveUiConfig { get; }
    public ReactiveCommand<Unit,Unit> RemoveUnusedMods { get; }
    public List<FieldElement> Fields { get; } = [];

    private async Task OnRemoveUnusedMods()
    {
        try
        {
            await _onBoarding.OnBoardingRemoveUnusedMods();
        }
        catch(OperationCanceledException) {}
    }

    private async void OnServerInstanceInstall(object? obj)
    {
        try
        {
            await _steamApi.UpdateServers();
        }
        catch (TrebException tex)
        {
            _logger.LogError(tex.Message);
            await _box.OpenErrorAsync(tex.Message);
        }
    }

    private void BuildFields()
    {
        Fields.Add(new TitleField().SetTitle(Resources.OnBoardingUsageChoice));
        Fields.Add(new ClientInstallationField(_onBoarding, _setup)
            .WhenFieldChanged(RequestAppRefresh)
            .SetTitle(Resources.SettingClientInstallation)
            .SetDescription(Resources.SettingClientInstallationText)
        );
        Fields.Add(new ServerInstallationField(_onBoarding, _setup)
            .WhenFieldChanged(RequestAppRefresh)
            .SetTitle(Resources.SettingServerInstanceCount)
            .SetDescription(Resources.SettingServerInstanceCountText)
        );
        Fields.Add(new TitleField().SetTitle(Resources.CatMiscellaneous));
        Fields.Add(new ToggleField()
            .WhenFieldChanged(SaveUiConfig)
            .SetTitle(Resources.SettingDisplayWarningOnKill)
            .SetDescription(Resources.SettingDisplayWarningOnKillText)
            .SetGetter(() => _uiConfig.DisplayWarningOnKill)
            .SetSetter((v) => _uiConfig.DisplayWarningOnKill = v)
            .SetDefault(() => UIConfig.DisplayWarningOnKillDefault)
        );
        Fields.Add(new TitleField().SetTitle(Resources.CatUpdates));
        Fields.Add(new ComboBoxField()
            .WhenFieldChanged(SaveConfig)
            .SetTitle(Resources.SettingAutoUpdateStatus)
            .SetDescription(Resources.SettingAutoUpdateStatusText)
            .AddOption(Resources.SettingAutoUpdateStatusNever)
            .AddOption(Resources.SettingAutoUpdateStatusLaunchOnly)
            .AddOption(Resources.SettingAutoUpdateStatusCheck)
            .SetGetter(() => _setup.Config.AutoUpdateStatus)
            .SetSetter((v) => _setup.Config.AutoUpdateStatus = v)
            .SetDefault(() => Config.AutoUpdateStatusDefault)
        );
        Fields.Add(new IntSliderField(120, 3600, 1)
            .WhenFieldChanged(SaveConfig)
            .SetTitle(Resources.SettingUpdateCheckInterval)
            .SetDescription(Resources.SettingUpdateCheckIntervalText)
            .SetGetter(() => _setup.Config.UpdateCheckInterval)
            .SetSetter((v) => _setup.Config.UpdateCheckInterval = v)
            .SetDefault(() => Config.UpdateCheckIntervalDefault)
        );
        Fields.Add(new ToggleField()
            .WhenFieldChanged(SaveConfig)
            .SetTitle(Resources.SettingVerifyAll)
            .SetDescription(Resources.SettingVerifyAllText)
            .SetGetter(() => _setup.Config.VerifyAll)
            .SetSetter((v) => _setup.Config.VerifyAll = v)
            .SetDefault(() => Config.VerifyAllDefault)
        );
        Fields.Add(new IntSliderField(1, 16, 1)
            .WhenFieldChanged(SaveConfig)
            .SetTitle(Resources.SettingMaxDownloads)
            .SetDescription(Resources.SettingMaxDownloadsText)
            .SetGetter(() => _setup.Config.MaxDownloads)
            .SetSetter((v) => _setup.Config.MaxDownloads = v)
            .SetDefault(() => Config.MaxDownloadsDefault)
        );
        Fields.Add(new IntSliderField(1, 16, 1)
            .WhenFieldChanged(SaveConfig)
            .SetTitle(Resources.SettingMaxServers)
            .SetDescription(Resources.SettingMaxServersText)
            .SetGetter(() => _setup.Config.MaxServers)
            .SetSetter((v) => _setup.Config.MaxServers = v)
            .SetDefault(() => Config.MaxServersDefault)
        );
        Fields.Add(new ToggleField()
            .WhenFieldChanged(SaveUiConfig)
            .SetTitle(Resources.SettingAutoRefreshModlist)
            .SetDescription(Resources.SettingAutoRefreshModlistText)
            .SetGetter(() => _uiConfig.AutoRefreshModlist)
            .SetSetter((v) => _uiConfig.AutoRefreshModlist = v)
            .SetDefault(() => UIConfig.AutoRefreshModlistDefault)
        );
    }    
}