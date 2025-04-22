using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using Humanizer;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.SettingFields;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    public class ClientProfilePanel : ReactiveObject, IRefreshablePanel
    {
        public ClientProfilePanel(
            DialogueBox box,
            AppSetup setup, 
            AppFiles appFiles, 
            ILogger<ClientProfilePanel> logger,
            UIConfig uiConfig)
        {
            _box = box;
            _setup = setup;
            _appFiles = appFiles;
            _logger = logger;
            _uiConfig = uiConfig;
            CanBeOpened = Tools.IsClientInstallValid(_setup.Config) && _setup.Config.ManageClient;
            
            LoadProfileList();
            _selectedProfile = _appFiles.Client.Resolve(_uiConfig.CurrentClientProfile);
            _profile = _appFiles.Client.Get(_selectedProfile);

            this.WhenAnyValue(x => x.SelectedProfile)
                .InvokeCommand(
                    ReactiveCommand.CreateFromTask<string>(OnProfileChanged));

            CreateProfileCommand = ReactiveCommand.CreateFromTask(OnProfileCreate);
            DeleteProfileCommand = ReactiveCommand.CreateFromTask(OnProfileDelete);
            DuplicateProfileCommand = ReactiveCommand.CreateFromTask(OnProfileDuplicate);
            OpenFolderProfileCommand = ReactiveCommand.Create(OnOpenFolderProfile);
            SaveProfile = ReactiveCommand.Create(() => _profile.SaveFile());
            
            BuildFields();
        }
        private readonly DialogueBox _box;
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly ILogger<ClientProfilePanel> _logger;
        private readonly UIConfig _uiConfig;
        private ClientProfile _profile;
        private string _profileSize = string.Empty;
        private string _selectedProfile;
        private bool _canBeOpened;

        public string Icon => @"mdi-controller";
        public string Label => Resources.PanelGameSaves;
        public ObservableCollection<FieldElement> Fields { get; } = [];
       
        public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFolderProfileCommand { get; }
        private ReactiveCommand<Unit, Unit> SaveProfile { get; }
        public ObservableCollectionExtended<string> Profiles { get; } = [];

        public string ProfileSize
        {
            get => _profileSize;
            protected set => this.RaiseAndSetIfChanged(ref _profileSize, value);
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                var resolved = _appFiles.Client.Resolve(value);
                this.RaiseAndSetIfChanged(ref _selectedProfile, resolved);
            }
        }

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public async Task RefreshPanel()
        {
            _logger.LogDebug(@"Refresh panel");
            CanBeOpened = Tools.IsClientInstallValid(_setup.Config) && _setup.Config.ManageClient;
            _profile = _appFiles.Client.Get(SelectedProfile);
            foreach (var f in Fields.OfType<IValueField>())
                f.Update.Execute().Subscribe();
            await RefreshProfileSize(SelectedProfile);
        }

        private async Task RefreshProfileSize(string profile)
        {
            var path = _appFiles.Client.GetDirectory(profile);
            var size = await Task.Run(() => Tools.DirectorySize(path));
            ProfileSize = size.Bytes().Humanize();
        }

        private void LoadProfileList()
        {
            Profiles.Clear();
            Profiles.AddRange(_appFiles.Client.GetList());
        }

        private void OnOpenFolderProfile()
        {
            string? folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            _logger.LogDebug(@"Opening folder {folder}", folder);
            Process.Start("explorer.exe", folder);
        }

        private async Task OnProfileChanged(string newProfile)
        {
            _logger.LogDebug(@"Swap profile to {profile}", newProfile);
            _uiConfig.CurrentClientProfile = SelectedProfile;
            _uiConfig.SaveFile();
            await RefreshPanel();
        }

        private async Task OnProfileCreate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _logger.LogInformation(@"Create profile {name}", name);
            _appFiles.Client.Create(name);
            LoadProfileList();
            SelectedProfile = name;
        }

        private Validation ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Validation.Invalid(Resources.ErrorNameEmpty);
            if (Profiles.Contains(name))
                return Validation.Invalid(Resources.ErrorNameAlreadyTaken);
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return Validation.Invalid(Resources.ErrorNameInvalidCharacters);
            return Validation.Valid;
        }

        private async Task OnProfileDelete()
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;

            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                    Resources.Deletion,
                    string.Format(Resources.DeletionText, SelectedProfile));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;
            
            _logger.LogInformation(@"Delete profile {name}", SelectedProfile);
            _appFiles.Client.Delete(SelectedProfile);

            LoadProfileList();
            SelectedProfile = string.Empty;
        }

        private async Task OnProfileDuplicate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _logger.LogInformation(@"Duplicate profile {selected} to {name}", _profile.ProfileName, name);
            await _appFiles.Client.Duplicate(_profile.ProfileName, name);
            LoadProfileList();
            SelectedProfile = name;
        }

        private async Task<string?> GetNewProfileName()
        {
            var modal = new OnBoardingNameSelection(Resources.Create, string.Empty)
                .SetValidation(ValidateName);
            await _box.OpenAsync(modal);
            return modal.Value;
        }

        private void BuildFields()
        {
            Fields.Add(new TitleField().SetTitle(Resources.CatGeneral));
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingBackgroundSound)
                .SetDescription(Resources.SettingBackgroundSoundText)
                .SetSetter((v) => _profile.BackgroundSound = v)
                .SetGetter(() => _profile.BackgroundSound)
                .SetDefault(() => ClientProfile.BackgroundSoundDefault)
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingIntroVid)
                .SetDescription(Resources.SettingIntroVidText)
                .SetGetter(() => _profile.RemoveIntroVideo)
                .SetSetter((v) => _profile.RemoveIntroVideo = v)
                .SetDefault(() => ClientProfile.RemoveIntroVideoDefault)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatProcessPerformance));
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingUseAllCore)
                .SetDescription(Resources.SettingUseAllCoreText)
                .SetGetter(() => _profile.UseAllCores)
                .SetSetter((v) => _profile.UseAllCores = v)
                .SetDefault(() => ClientProfile.UseAllCoresDefault)
            );
            Fields.Add(new ComboBoxField()
                .WhenFieldChanged(SaveProfile)
                .SetDescription(Resources.SettingProcessPrioText)
                .SetTitle(Resources.SettingProcessPrio)
                .AddOption(Resources.SettingProcessPrioNormal)
                .AddOption(Resources.SettingProcessPrioAboveNormal)
                .AddOption(Resources.SettingProcessPrioHigh)
                .AddOption(Resources.SettingProcessPrioRealtime)
                .SetGetter(() => _profile.ProcessPriority)
                .SetSetter((v) => _profile.ProcessPriority = v)
                .SetDefault(() => ClientProfile.ProcessPriorityDefault)
            );
            Fields.Add(new CpuAffinityField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingCpuAffinity)
                .SetDescription(Resources.SettingCpuAffinityText)
                .SetSetter((v) => _profile.CPUThreadAffinity = v)
                .SetGetter(() => _profile.CPUThreadAffinity)
                .SetDefault(CpuAffinityField.DefaultValue)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatGraphics));
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingUltraAniso)
                .SetDescription(Resources.SettingUltraAnisoText)
                .SetGetter(() => _profile.UltraAnisotropy)
                .SetSetter((v) => _profile.UltraAnisotropy = v)
                .SetDefault(() => ClientProfile.UltraAnisotropyDefault));
            Fields.Add(new IntSliderField(0, 4000, 100)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingTexPool)
                .SetDescription(Resources.SettingTexPoolText)
                .SetGetter(() => _profile.AddedTexturePool)
                .SetSetter((v) => _profile.AddedTexturePool = v)
                .SetDefault(() => ClientProfile.AddedTexturePoolDefault)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatMiscellaneous));
            if(_setup.Experiment)
                Fields.Add(new ToggleField()
                    .WhenFieldChanged(SaveProfile)
                    .SetExperiment()
                    .SetTitle(Resources.SettingAsyncScene)
                    .SetDescription(Resources.SettingAsyncSceneText)
                    .SetGetter(() => _profile.EnableAsyncScene)
                    .SetSetter((v) => _profile.EnableAsyncScene = v)
                    .SetDefault(() => ClientProfile.EnableAsyncSceneDefault)
                );
            if(_setup.Experiment)
                Fields.Add(new IntSliderField(10000, 100000, 1000)
                    .WhenFieldChanged(SaveProfile)
                    .SetExperiment()
                    .SetTitle(Resources.SettingInternetSpeed)
                    .SetDescription(Resources.SettingInternetSpeedText)
                    .SetGetter(() => _profile.ConfiguredInternetSpeed)
                    .SetSetter((v) => _profile.ConfiguredInternetSpeed = v)
                    .SetDefault(() => ClientProfile.ConfiguredInternetSpeedDefault)
                );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingLog)
                .SetDescription(Resources.SettingLogText)
                .SetGetter(() => _profile.Log)
                .SetSetter((v) => _profile.Log = v)
                .SetDefault(() => ClientProfile.LogDefault)
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingAdminServerList)
                .SetDescription(Resources.SettingAdminServerListText)
                .SetGetter(() => _profile.TotAdminDoNotLoadServerList)
                .SetSetter((v) => _profile.TotAdminDoNotLoadServerList = v)
                .SetDefault(() => ClientProfile.TotAdminDoNotLoadServerListDefault)
            );
            Fields.Add(new MultiLineTextField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingLogFilter)
                .SetDescription(Resources.SettingLogFilterText)
                .SetGetter(() => string.Join(Environment.NewLine, _profile.LogFilters))
                .SetSetter((v) => _profile.LogFilters = v.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList())
                .SetDefault(() => string.Join(Environment.NewLine, ClientProfile.LogFiltersDefault))
            );
        }


    }
}