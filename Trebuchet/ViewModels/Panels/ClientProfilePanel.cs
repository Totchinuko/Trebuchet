using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Humanizer;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.SettingFields;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.Panels
{
    public class ClientProfilePanel : Panel
    {
        private readonly DialogueBox _box;
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private ClientProfile _profile;
        private string _profileSize = new(string.Empty);
        private string _selectedProfile = new(string.Empty);

        public ClientProfilePanel(
            DialogueBox box,
            AppSetup setup, 
            AppFiles appFiles, 
            UIConfig uiConfig) : 
            base(Resources.PanelGameSaves, "mdi-controller", false)
        {
            _box = box;
            _setup = setup;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            LoadProfile(
                _appFiles.Client.ResolveProfile(
                    _uiConfig.CurrentClientProfile));
            LoadProfileList();

            this.WhenAnyValue(x => x.SelectedProfile)
                .Subscribe(OnProfileChanged);

            CreateProfileCommand = ReactiveCommand.Create(OnProfileCreate);
            DeleteProfileCommand = ReactiveCommand.Create(OnProfileDelete);
            DuplicateProfileCommand = ReactiveCommand.Create(OnProfileDuplicate);
            OpenFolderProfileCommand = ReactiveCommand.Create(OnOpenFolderProfile);
            SaveProfile = ReactiveCommand.Create(() => _profile.SaveFile());
            RefreshPanel.IsExecuting
                .Where(x => x)
                .Select(_ => Tools.IsClientInstallValid(setup.Config) && setup.Config.ManageClient)
                .Subscribe(x => CanTabBeClicked = x);
            
            RefreshPanel.Subscribe((_) =>
            {
                LoadProfile(SelectedProfile);
                LoadProfileList();
            });
            
            BuildFields();
        }

        public ObservableCollection<FieldElement> Fields { get; } = [];
       
        public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFolderProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveProfile { get; }
        public ObservableCollection<string> Profiles { get; } = [];

        public string ProfileSize
        {
            get => _profileSize;
            protected set => this.RaiseAndSetIfChanged(ref _profileSize, value);
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
        }

        [MemberNotNull("_profile")]
        private void LoadProfile(string profile)
        {
            _profile = _appFiles.Client.Get(profile);
            RefreshProfileSize(profile);
            foreach (var f in Fields.OfType<IValueField>())
                f.Update.Execute().Subscribe();
        }

        private async void RefreshProfileSize(string profile)
        {
            var path = _appFiles.Client.GetFolder(profile);
            var size = await Task.Run(() => Tools.DirectorySize(path));
            ProfileSize = size.Bytes().Humanize();
        }

        private void LoadProfileList()
        {
            Profiles.Clear();
            Profiles.AddRange(_appFiles.Client.ListProfiles());
        }

        private void OnOpenFolderProfile()
        {
            string? folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            Process.Start("explorer.exe", folder);
        }

        private void OnProfileChanged(string newProfile)
        {
            var resolved = _appFiles.Client.ResolveProfile(newProfile);
            if (resolved != newProfile)
            {
                SelectedProfile = resolved;
                return;
            }

            _uiConfig.CurrentClientProfile = SelectedProfile;
            _uiConfig.SaveFile();
            LoadProfile(newProfile);
        }

        private async void OnProfileCreate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
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

        private async void OnProfileDelete()
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;

            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                    Resources.Deletion,
                    string.Format(Resources.DeletionText, SelectedProfile));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;
            
            _appFiles.Client.Delete(_profile.ProfileName);

            LoadProfileList();
            SelectedProfile = string.Empty;
        }

        private async void OnProfileDuplicate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
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
                .SetDefault(() => CpuAffinityField.DefaultValue())
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
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingAsyncScene)
                .SetDescription(Resources.SettingAsyncSceneText)
                .SetGetter(() => _profile.EnableAsyncScene)
                .SetSetter((v) => _profile.EnableAsyncScene = v)
                .SetDefault(() => ClientProfile.EnableAsyncSceneDefault)
            );
            if(_setup.Experiment)
                Fields.Add(new IntSliderField(10000, 100000, 1000)
                    .WhenFieldChanged(SaveProfile)
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
                .SetSetter((v) => _profile.LogFilters = v.Split(Environment.NewLine).ToList())
                .SetDefault(() => string.Join(Environment.NewLine, ClientProfile.LogFiltersDefault))
            );
        }
    }
}