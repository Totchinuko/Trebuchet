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
    public class ServerProfilePanel : ReactiveObject, IRefreshablePanel, IDisplablePanel
    {
        private readonly DialogueBox _box;
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private readonly ILogger<ServerProfilePanel> _logger;
        private ServerProfile _profile;
        private string _selectedProfile;
        private string _profileSize = string.Empty;
        private bool _canBeOpened;

        public ServerProfilePanel(
            DialogueBox box,
            AppSetup setup,
            AppFiles appFiles,
            UIConfig uiConfig,
            ILogger<ServerProfilePanel> logger
            )
        {
            _box = box;
            _setup = setup;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            _logger = logger;
            CanBeOpened = Tools.IsServerInstallValid(_setup.Config);

            LoadProfileList();
            _selectedProfile = _appFiles.Server.Resolve(_uiConfig.CurrentServerProfile);
            _profile = _appFiles.Server.Get(_selectedProfile);
            
            this.WhenAnyValue(x => x.SelectedProfile)
                .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnProfileChanged));
            
            CreateProfileCommand = ReactiveCommand.CreateFromTask(OnProfileCreate);
            DeleteProfileCommand = ReactiveCommand.CreateFromTask(OnProfileDelete);
            DuplicateProfileCommand = ReactiveCommand.CreateFromTask(OnProfileDuplicate);
            OpenFolderProfileCommand = ReactiveCommand.Create(OnOpenFolderProfile);
            SaveProfile = ReactiveCommand.Create(() => _profile.SaveFile());
           
            BuildFields();
        }

        public ObservableCollection<FieldElement> Fields { get; } = [];
        public ObservableCollectionExtended<string> Profiles { get; } = [];

        public ReactiveCommand<Unit,Unit> CreateProfileCommand { get; }
        public ReactiveCommand<Unit,Unit> DeleteProfileCommand { get; }
        public ReactiveCommand<Unit,Unit> DuplicateProfileCommand { get; }
        public ReactiveCommand<Unit,Unit> OpenFolderProfileCommand { get; }
        public ReactiveCommand<Unit,Unit> SaveProfile { get; }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                var resolved = _appFiles.Server.Resolve(value);
                this.RaiseAndSetIfChanged(ref _selectedProfile, resolved);
            }
        }

        public string ProfileSize
        {
            get => _profileSize;
            set => this.RaiseAndSetIfChanged(ref _profileSize, value);
        }

        public string Icon => @"mdi-server-network";
        public string Label => Resources.PanelServerSaves;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public async Task RefreshPanel()
        {
            _logger.LogDebug(@"Refresh panel");
            CanBeOpened = Tools.IsServerInstallValid(_setup.Config);
            _profile = _appFiles.Server.Get(SelectedProfile);
            await RefreshProfileSize(SelectedProfile);
            foreach (var f in Fields.OfType<IValueField>())
                f.Update.Execute().Subscribe();
        }

        public async Task DisplayPanel()
        {
            _logger.LogDebug(@"Display panel");
            await RefreshProfileSize(SelectedProfile);
        }
        
        private async Task RefreshProfileSize(string profile)
        {
            var path = _appFiles.Server.GetDirectory(profile);
            var size = await Task.Run(() => Tools.DirectorySize(path));
            ProfileSize = size.Bytes().Humanize();
        }

        private void LoadProfileList()
        {
            Profiles.Clear();
            Profiles.AddRange(_appFiles.Server.GetList());
        }

        private void OnOpenFolderProfile()
        {
            var folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            _logger.LogDebug(@"Opening folder {folder}", folder);
            Process.Start("explorer.exe", folder);
        }

        private async Task OnProfileChanged(string newProfile)
        {
            _logger.LogDebug(@"Swap profile {profile}", newProfile);
            _uiConfig.CurrentServerProfile = newProfile;
            _uiConfig.SaveFile();
            await RefreshPanel();
        }

        private async Task OnProfileCreate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _logger.LogInformation(@"Create profile {name}", name);
            _appFiles.Server.Create(name);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private async Task OnProfileDelete()
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;

            var confirm = new OnBoardingConfirmation(
                Resources.Deletion,
                string.Format(Resources.DeletionText, SelectedProfile));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;
            
            _logger.LogInformation(@"Delete profile {name}", SelectedProfile);
            _appFiles.Server.Delete(SelectedProfile);

            LoadProfileList();
            SelectedProfile = string.Empty;
        }

        private async Task OnProfileDuplicate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _logger.LogInformation(@"Duplicate profile {from} to {to}", _profile.ProfileName, name);
            await _appFiles.Server.Duplicate(_profile.ProfileName, name);
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

        private void BuildFields()
        {
            Fields.Add(new TitleField().SetTitle(Resources.CatServerSettings));
            Fields.Add(new TextField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerName)
                .SetDescription(Resources.SettingServerNameText)
                .SetGetter(() => _profile.ServerName)
                .SetSetter((v) => _profile.ServerName = v)
                .SetDefault(() => ServerProfile.ServerNameDefault)
            );
            Fields.Add(new PasswordField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerPass)
                .SetDescription(Resources.SettingServerPassText)
                .SetGetter(() => _profile.ServerPassword)
                .SetSetter((v) => _profile.ServerPassword = v)
                .SetDefault(() => ServerProfile.ServerPasswordDefault)
            );
            Fields.Add(new PasswordField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerAdminPass)
                .SetDescription(Resources.SettingServerAdminPassText)
                .SetGetter(() => _profile.AdminPassword)
                .SetSetter((v) => _profile.AdminPassword = v)
                .SetDefault(() => ServerProfile.AdminPasswordDefault)
            );
            Fields.Add(new IntField(0,int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerMaxPlayer)
                .SetDescription(Resources.SettingServerMaxPlayerText)
                .SetGetter(() => _profile.MaxPlayers)
                .SetSetter((v) => _profile.MaxPlayers = v)
                .SetDefault(() => ServerProfile.MaxPlayersDefault)
            );
            Fields.Add(new ComboBoxField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerRegion)
                .SetDescription(Resources.SettingServerRegionText)
                .AddOption(Resources.SettingServerRegionEurope)
                .AddOption(Resources.SettingServerRegionNorthAmerica)
                .AddOption(Resources.SettingServerRegionAsia)
                .AddOption(Resources.SettingServerRegionAustralia)
                .AddOption(Resources.SettingServerRegionSouthAmerica)
                .AddOption(Resources.SettingServerRegionJapan)
                .SetGetter(() => _profile.ServerRegion)
                .SetSetter((v) => _profile.ServerRegion = v)
                .SetDefault(() => ServerProfile.ServerRegionDefault)
            );
            Fields.Add(new MapField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerMap)
                .SetDescription(Resources.SettingServerMapText)
                .SetGetter(() => _profile.Map)
                .SetSetter((v) => _profile.Map = v)
                .SetDefault(() => ServerProfile.MapDefault)
            );
            Fields.Add(new MultiLineTextField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingSudoAdminList)
                .SetDescription(Resources.SettingSudoAdminListText)
                .SetGetter(() => string.Join(Environment.NewLine, _profile.SudoSuperAdmins))
                .SetSetter((v) => _profile.SudoSuperAdmins = v.Split(Environment.NewLine).ToList())
                .SetDefault(() => string.Join(Environment.NewLine, ServerProfile.SudoSuperAdminsDefault))
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingTotAdminPrecision)
                .SetDescription(Resources.SettingTotAdminPrecisionText)
                .SetGetter(() => _profile.DisableHighPrecisionMoveTool)
                .SetSetter((v) => _profile.DisableHighPrecisionMoveTool = v)
                .SetDefault(() => ServerProfile.DisableHighPrecisionMoveToolDefault)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatRestartSettings));
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerKillZombies)
                .SetDescription(Resources.SettingServerKillZombiesText)
                .SetGetter(() => _profile.KillZombies)
                .SetSetter((v) => _profile.KillZombies = v)
                .SetDefault(() => ServerProfile.KillZombiesDefault)
            );
            Fields.Add(new IntField(30, int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerZombieDuration)
                .SetDescription(Resources.SettingServerZombieDurationText)
                .SetGetter(() => _profile.ZombieCheckSeconds)
                .SetSetter((v) => _profile.ZombieCheckSeconds = v)
                .SetDefault(() => ServerProfile.ZombieCheckSecondsDefault)
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerCrashRestart)
                .SetDescription(Resources.SettingServerCrashRestartText)
                .SetGetter(() => _profile.RestartWhenDown)
                .SetSetter((v) => _profile.RestartWhenDown = v)
                .SetDefault(() => ServerProfile.RestartWhenDownDefault)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatPerformance));
            Fields.Add(new IntField(1, int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerMaximumTickRate)
                .SetDescription(Resources.SettingServerMaximumTickRateText)
                .SetGetter(() => _profile.MaximumTickRate)
                .SetSetter((v) => _profile.MaximumTickRate = v)
                .SetDefault(() => ServerProfile.MaximumTickRateDefault)
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerUseAllCores)
                .SetDescription(Resources.SettingServerUseAllCoresText)
                .SetGetter(() => _profile.UseAllCores)
                .SetSetter((v) => _profile.UseAllCores = v)
                .SetDefault(() => ServerProfile.UseAllCoresDefault)
            );
            Fields.Add(new ComboBoxField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerProcessPriority)
                .SetDescription(Resources.SettingServerProcessPriorityText)
                .AddOption(Resources.SettingProcessPrioNormal)
                .AddOption(Resources.SettingProcessPrioAboveNormal)
                .AddOption(Resources.SettingProcessPrioHigh)
                .AddOption(Resources.SettingProcessPrioRealtime)
                .SetGetter(() => _profile.ProcessPriority)
                .SetSetter((v) => _profile.ProcessPriority = v)
                .SetDefault(() => ServerProfile.ProcessPriorityDefault)
            );
            Fields.Add(new CpuAffinityField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerCPUThreadAffinity)
                .SetDescription(Resources.SettingServerCPUThreadAffinityText)
                .SetGetter(() => _profile.CPUThreadAffinity)
                .SetSetter((v) => _profile.CPUThreadAffinity = v)
                .SetDefault(() => ServerProfile.CPUThreadAffinityDefault)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatPorts));
            var gameClientPort = new IntField(int.MinValue, int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerGameClientPort)
                .SetDescription(Resources.SettingServerGameClientPortText)
                .SetGetter(() => _profile.GameClientPort)
                .SetSetter((v) => _profile.GameClientPort = v)
                .SetDefault(() => ServerProfile.GameClientPortDefault);
            Fields.Add(gameClientPort);
            Fields.Add(new IntField(int.MinValue, int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerRawUDPPort)
                .SetDescription(Resources.SettingServerRawUDPPortText)
                .SetGetter(() => _profile.GameClientPort+1)
                .SetDefault(() => ServerProfile.GameClientPortDefault+1)
                .SetEnabled(false)
                .UpdateWith(gameClientPort)
            );
            Fields.Add(new IntField(int.MinValue, int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerSourceQueryPort)
                .SetDescription(Resources.SettingServerSourceQueryPortText)
                .SetGetter(() => _profile.SourceQueryPort)
                .SetSetter((v) => _profile.SourceQueryPort = v)
                .SetDefault(() => ServerProfile.SourceQueryPortDefault)
            );
            var multiHome = new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerEnableMultiHome)
                .SetDescription(Resources.SettingServerEnableMultiHomeText)
                .SetGetter(() => _profile.EnableMultiHome)
                .SetSetter((v) => _profile.EnableMultiHome = v)
                .SetDefault(() => ServerProfile.EnableMultiHomeDefault);
            Fields.Add(multiHome);
            var multiHomeAdress = new TextField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerMultiHomeAddress)
                .SetDescription(Resources.SettingServerMultiHomeAddressText)
                .SetGetter(() => _profile.MultiHomeAddress)
                .SetSetter((v) => _profile.MultiHomeAddress = v)
                .SetDefault(() => ServerProfile.MultiHomeAddressDefault);
            Fields.Add(multiHomeAdress);
            multiHome.WhenAnyValue(x => x.Value).Subscribe(x => multiHomeAdress.IsVisible = x);
            
            Fields.Add(new TitleField().SetTitle(Resources.CatRCon));
            var rcon = new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerEnableRCon)
                .SetDescription(Resources.SettingServerEnableRConText)
                .SetGetter(() => _profile.EnableRCon)
                .SetSetter((v) => _profile.EnableRCon = v)
                .SetDefault(() => ServerProfile.EnableRConDefault);
            Fields.Add(rcon);
            var rconPort = new IntField(0, 65535)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerRConPort)
                .SetDescription(Resources.SettingServerRConPortText)
                .SetGetter(() => _profile.RConPort)
                .SetSetter((v) => _profile.RConPort = v)
                .SetDefault(() => ServerProfile.RConPortDefault);
            var rconPass = new PasswordField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerRConPassword)
                .SetDescription(Resources.SettingServerRConPasswordText)
                .SetGetter(() => _profile.RConPassword)
                .SetSetter((v) => _profile.RConPassword = v)
                .SetDefault(() => ServerProfile.RConPasswordDefault);
            var rconKarma = new IntField(0, int.MaxValue)
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerRConMaxKarma)
                .SetDescription(Resources.SettingServerRConMaxKarmaText)
                .SetGetter(() => _profile.RConMaxKarma)
                .SetSetter((v) => _profile.RConMaxKarma = v)
                .SetDefault(() => ServerProfile.RConMaxKarmaDefault);
            Fields.Add(rconPort);
            Fields.Add(rconPass);
            Fields.Add(rconKarma);
            rcon.WhenAnyValue(x => x.Value)
                .Subscribe(x =>
                {
                    rconPass.IsVisible = x;
                    rconPort.IsVisible = x;
                    rconKarma.IsVisible = x;
                });
                
            Fields.Add(new TitleField().SetTitle(Resources.CatAntiCheat));
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerEnableVAC)
                .SetDescription(Resources.SettingServerEnableVACText)
                .SetGetter(() => _profile.EnableVAC)
                .SetSetter((v) => _profile.EnableVAC = v)
                .SetDefault(() => ServerProfile.EnableVACDefault)
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerEnableBattleEye)
                .SetDescription(Resources.SettingServerEnableBattleEyeText)
                .SetGetter(() => _profile.EnableBattleEye)
                .SetSetter((v) => _profile.EnableBattleEye = v)
                .SetDefault(() => ServerProfile.EnableBattleEyeDefault)
            );
            Fields.Add(new TitleField().SetTitle(Resources.CatMiscellaneous));
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerLog)
                .SetDescription(Resources.SettingServerLogText)
                .SetGetter(() => _profile.Log)
                .SetSetter((v) => _profile.Log = v)
                .SetDefault(() => ServerProfile.LogDefault)
            );
            Fields.Add(new ToggleField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerNoAISpawn)
                .SetDescription(Resources.SettingServerNoAISpawnText)
                .SetGetter(() => _profile.NoAISpawn)
                .SetSetter((v) => _profile.NoAISpawn = v)
                .SetDefault(() => ServerProfile.NoAISpawnDefault)
            );
            Fields.Add(new MultiLineTextField()
                .WhenFieldChanged(SaveProfile)
                .SetTitle(Resources.SettingServerLogFilters)
                .SetDescription(Resources.SettingServerLogFiltersText)
                .SetGetter(() => string.Join(Environment.NewLine, _profile.LogFilters))
                .SetSetter((v) => _profile.LogFilters = v.Split(Environment.NewLine).ToList())
                .SetDefault(() => string.Join(Environment.NewLine, ServerProfile.LogFiltersDefault))
            );
        }
    }
}