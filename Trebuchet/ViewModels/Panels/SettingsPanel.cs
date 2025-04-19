using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData.Binding;
using ReactiveUI;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Services.Language;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.SettingFields;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels;

public class SettingsPanel : ReactiveObject, IRefreshingPanel, IBottomPanel
{


    public SettingsPanel(
        AppSetup setup, 
        OnBoarding onBoarding,
        ILanguageManager langManager,
        DialogueBox box,
        TaskBlocker blocker,
        UIConfig uiConfig)
    {
        _setup = setup;
        _uiConfig = uiConfig;
        _onBoarding = onBoarding;
        _langManager = langManager;
        _box = box;
        _blocker = blocker;

        AvailableLocales.AddRange(langManager.AllLanguages);
        _selectedLanguage = langManager.AllLanguages.Where(x => x.Code == _uiConfig.UICulture)
            .FirstOrDefault(langManager.DefaultLanguage);

        SaveConfig = ReactiveCommand.Create(() => _setup.Config.SaveFile());
        SaveUiConfig = ReactiveCommand.Create(() => _uiConfig.SaveFile());
        ChangeLanguage = ReactiveCommand.CreateFromTask<LanguageModel?>(OnLanguageChanged);
        ChangePlateformTheme = ReactiveCommand.Create(OnPlateformThemeChanged);
        RestartProcess = ReactiveCommand.CreateFromTask(OnRestartProcess);
        
        this.WhenValueChanged<SettingsPanel, LanguageModel>(x => x.SelectedLanguage, false, () => _langManager.DefaultLanguage)
            .InvokeCommand(ChangeLanguage);
            
        BuildFields();
    }
    
    private readonly AppSetup _setup;
    private readonly UIConfig _uiConfig;
    private readonly OnBoarding _onBoarding;
    private readonly ILanguageManager _langManager;
    private readonly DialogueBox _box;
    private readonly TaskBlocker _blocker;
    private LanguageModel _selectedLanguage;
    private bool _canBeOpened = true;

    public ReactiveCommand<Unit,Unit> SaveConfig { get; }
    public ReactiveCommand<Unit,Unit> SaveUiConfig { get; }
    public ReactiveCommand<Unit, Unit> ChangePlateformTheme { get; }
    public ReactiveCommand<LanguageModel?,Unit> ChangeLanguage { get; }
    public ReactiveCommand<Unit,Unit> RestartProcess { get; }
    public List<FieldElement> Fields { get; } = [];
    public ObservableCollectionExtended<LanguageModel> AvailableLocales { get; } = [];

    public LanguageModel SelectedLanguage
    {
        get => _selectedLanguage;
        set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
    }

    public string Icon => @"mdi-cog";
    public string Label => Resources.PanelSettings;

    public bool CanBeOpened
    {
        get => _canBeOpened;
        set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
    }

    public event AsyncEventHandler? RequestRefresh;

    private async Task OnLanguageChanged(LanguageModel? model)
    {
        if (model is null) return;
        _uiConfig.UICulture = model.Code;
        _langManager.SetLanguage(model.Code);
        _uiConfig.SaveFile();
        if (_blocker.IsSet<SteamDownload>())
        {
            var message = new OnBoardingMessage(Resources.OnBoardingLanguageChange, Resources.OnBoardingLanguageChangeMessage);
            await _box.OpenAsync(message);
        }
        else
        {
            var confirm = new OnBoardingConfirmation(Resources.OnBoardingLanguageChange,
                Resources.OnBoardingLanguageChangeConfirm);
            await _box.OpenAsync(confirm);
            if(confirm.Result)
                Utils.Utils.RestartProcess(_setup);
        }
    }

    public async Task OnRestartProcess()
    {
        if (_blocker.IsSet<SteamDownload>())
        {
            var message = new OnBoardingMessage(Resources.OnBoardingRestartProcess, Resources.OnBoardingRestartProcessSubMessage);
            await _box.OpenAsync(message);
        }
        else
        {
            var confirm = new OnBoardingConfirmation(Resources.OnBoardingRestartProcess,
                Resources.OnBoardingRestartProcessSub);
            await _box.OpenAsync(confirm);
            if(confirm.Result)
                Utils.Utils.RestartProcess(_setup);
        }
    }

    private void OnPlateformThemeChanged()
    {
        Utils.Utils.ApplyPlateformTheme((PlateformTheme)_uiConfig.PlateformTheme);
    }

    private void BuildFields()
    {
        Fields.Add(new TitleField().SetTitle(Resources.OnBoardingUsageChoice));
        Fields.Add(new ClientInstallationField(_onBoarding, _setup)
            .WhenFieldChanged(ReactiveCommand.CreateFromTask(OnRequestRefresh))
            .SetTitle(Resources.SettingClientInstallation)
            .SetDescription(Resources.SettingClientInstallationText)
        );
        Fields.Add(new ServerInstallationField(_onBoarding, _setup)
            .WhenFieldChanged(ReactiveCommand.CreateFromTask(OnRequestRefresh))
            .SetTitle(Resources.SettingServerInstanceCount)
            .SetDescription(Resources.SettingServerInstanceCountText)
        );
        Fields.Add(new AppDataDirectoryField(_onBoarding, _setup)
            .SetTitle(Resources.OnBoardingDataDirectory)
            .SetDescription(Resources.OnBoardingDataDirectorySub)
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
        Fields.Add(new ComboBoxField()
            .WhenFieldChanged(ChangePlateformTheme)
            .WhenFieldChanged(SaveUiConfig)
            .SetTitle(Resources.SettingPlateformTheme)
            .SetDescription(Resources.SettingPlateformThemeText)
            .AddOption(Resources.SettingPlateformThemeDefault)
            .AddOption(Resources.SettingPlateformThemeDark)
            .AddOption(Resources.SettingPlateformThemeLight)
            .SetGetter(() => _uiConfig.PlateformTheme)
            .SetSetter((v) => _uiConfig.PlateformTheme = v)
            .SetDefault(() => UIConfig.PlatformThemeDefault)
        );
        Fields.Add(new ToggleField()
            .WhenFieldChanged(SaveUiConfig)
            .WhenFieldChanged(RestartProcess)
            .SetTitle(Resources.SettingExperiments)
            .SetDescription(Resources.SettingExperimentsText)
            .SetGetter(() => _uiConfig.Experiments)
            .SetSetter((v) => _uiConfig.Experiments = v)
            .SetDefault(() => UIConfig.ExperimentsDefault)
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

    private async Task OnRequestRefresh()
    {
        if (RequestRefresh is not null)
            await RequestRefresh.Invoke(this, EventArgs.Empty);
    }
}