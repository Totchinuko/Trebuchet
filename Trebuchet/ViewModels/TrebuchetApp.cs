using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;
using Panel = Trebuchet.ViewModels.Panels.Panel;

namespace Trebuchet.ViewModels
{
    public sealed class TrebuchetApp : ReactiveObject
    {
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly Launcher _launcher;
        private readonly UIConfig _uiConfig;
        private readonly Steam _steam;
        private Panel _activePanel;
        private List<Panel> _panels;
        private DispatcherTimer _timer;
        private bool _foldedMenu;

        public TrebuchetApp(
            AppSetup setup,
            AppFiles appFiles,
            Launcher launcher, 
            UIConfig uiConfig,
            Steam steam, 
            SteamWidget steamWidget,
            DialogueBox dialogueBox,
            IEnumerable<Panel> panels)
        {
            _setup = setup;
            _appFiles = appFiles;
            _launcher = launcher;
            _uiConfig = uiConfig;
            _steam = steam;
            _panels = panels.ToList();
            SteamWidget = steamWidget;
            DialogueBox = dialogueBox;

            ToggleFoldedCommand = ReactiveCommand.Create(() =>
            {
                FoldedMenu = !FoldedMenu;
                _uiConfig.FoldedMenu = FoldedMenu;
                _uiConfig.SaveFile();
            });

            InitializePanels(_panels);
            _activePanel = BottomPanels.First(x => x.CanTabBeClicked);
            
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimerTick);
            _timer.Start();
            
            OnBoardingActions();
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {
            _timer.Stop();
            await _launcher.Tick();
            foreach (var panel in _panels)
                await panel.Tick();
            _timer.Start();
        }

        public static string AppTitle => $"Tot ! Trebuchet {TrebuchetUtils.Utils.GetFileVersion()}";

        public bool FoldedMenu
        {
            get => _foldedMenu;
            set => this.RaiseAndSetIfChanged(ref _foldedMenu, value);
        }
        
        public ReactiveCommand<Unit, Unit> ToggleFoldedCommand { get; }
        
        public Panel ActivePanel
        {
            get => _activePanel;
            set
            {
                if(_activePanel == value) return;
                _activePanel.Active = false;
                this.RaiseAndSetIfChanged(ref _activePanel, value);
                _activePanel.Active = true;
                _activePanel.DisplayPanel.Execute();
            }
        }

        public ObservableCollection<Panel> TopPanels { get; } = [];
        public ObservableCollection<Panel> BottomPanels { get; } = [];

        public SteamWidget SteamWidget { get; }
        
        public DialogueBox DialogueBox { get; }

        public async void OnWindowShow()
        {
            _panels.ForEach(x => x.OnWindowShow());
            await _steam.Connect();
        }
        
        internal void OnAppClose()
        {
            _launcher.Dispose();
            _steam.Disconnect();
            Task.Run(() =>
            {
                while (_steam.IsConnected)
                    Task.Delay(100);
            }).Wait();
        }

        private void InitializePanels(List<Panel> panels)
        {
            foreach (var panel in panels)
            {
                panel.TabClick.Subscribe((p) =>
                {
                    ActivePanel = p;
                });
                panel.RequestAppRefresh.Subscribe((_) => RefreshPanels());
                
                if(panel.BottomPosition)
                    BottomPanels.Add(panel);
                else
                    TopPanels.Add(panel);
            }
        }

        private void RefreshPanels()
        {
            foreach (var panel in _panels)
                panel.RefreshPanel.Execute();
        }

        private async void OnBoardingActions()
        {
            _appFiles.SetupFolders();
            if (!await OnBoardingCheckTrebuchet()) return;
            if (!await OnBoardingFirstLaunch()) return;
            _activePanel.Active = true;
            _activePanel.DisplayPanel.Execute();
        }

        private async Task<bool> OnBoardingFirstLaunch()
        {
            var configPath = AppConstants.GetConfigPath(_setup.IsTestLive);
            if(File.Exists(configPath)) return true;
            if (!await OnBoardingUsageChoice()) return false;
            _setup.Config.SaveFile();
            RefreshPanels();
            return true;
        }

        private async Task<bool> OnBoardingUsageChoice()
        {
            var choice = new OnBoardingBranch(Resources.OnBoardingUsageChoice, Resources.OnBoardingUsageChoiceText)
                .SetSize<OnBoardingBranch>(750, 400)
                .AddChoice(Resources.OnBoardingUsageChoicePlayer, Resources.OnBoardingUsageChoicePlayerSub)
                .AddChoice(Resources.OnBoardingUsageChoiceServer, Resources.OnBoardingUsageChoiceServerSub)
                .AddChoice(Resources.OnBoardingUsageChoiceModder, Resources.OnBoardingUsageChoiceModderSub);
            await DialogueBox.OpenAsync(choice);
            switch (choice.Result)
            {
                case 0:
                    _setup.Config.AutoUpdateStatus = AutoUpdateStatus.Never;
                    _setup.Config.ServerInstanceCount = 0;
                    return await OnBoardingFindConanExile();
                case 1:
                    _setup.Config.AutoUpdateStatus = AutoUpdateStatus.CheckForUpdates;
                    _setup.Config.ServerInstanceCount = 1;
                    return await OnBoardingServerDownload();
                case 2:
                    _setup.Config.AutoUpdateStatus = AutoUpdateStatus.Never;
                    _setup.Config.ServerInstanceCount = 1;
                    if(!await OnBoardingFindConanExile()) return false;
                    return await OnBoardingServerDownload();
                default:
                    throw new Exception("OnBoarding Failed");
            }
        }

        private async Task<bool> OnBoardingServerDownload()
        {
            var progress = new OnBoardingProgress(Resources.UpdateServersLabel, string.Empty)
                .SetSize<OnBoardingProgress>(600, 250);
            DialogueBox.Open(progress);
            _steam.SetTemporaryProgress(progress);
            if(!SteamWidget.IsConnected)
                await _steam.Connect();
            var cts = new CancellationTokenSource();
            await _steam.UpdateServerInstances(cts);
            progress.Progress = 1.0;
            _steam.RestoreProgress();
            progress.Close();
            return true;
        }

        private async Task<bool> OnBoardingFindConanExile()
        {
            if (Tools.IsClientInstallValid(_setup.Config.ClientPath))
                return await OnBoardingApplyConanManagement();
            
            var finder = new OnBoardingDirectory(Resources.OnBoardingLocateConan, Resources.OnBoardingLocateConanText)
                .SetValidation(ValidateConanExileLocation)
                .SetSize<OnBoardingDirectory>(600, 200);
            await DialogueBox.OpenAsync(finder);
            if(finder.Value is null) throw new Exception("OnBoarding Failed");
            _setup.Config.ClientPath = finder.Value;
            return await OnBoardingAllowConanManagement();
        }

        private Validation ValidateConanExileLocation(string? path)
        {
            if(string.IsNullOrEmpty(path))
                return Validation.Invalid(Resources.ErrorValueEmpty);
            if (!Tools.IsClientInstallValid(path))
                return new Validation(false, Resources.OnBoardingLocateConanError);
            return new Validation(true, string.Empty);
        }

        private async Task<bool> OnBoardingAllowConanManagement()
        {
            var choice = new OnBoardingBranch(Resources.OnBoardingManageConan, Resources.OnBoardingManageConanText)
                .SetSize<OnBoardingBranch>(650, 300)
                .AddChoice(Resources.OnBoardingManageConanNo, Resources.OnBoardingManageConanNoSub)
                .AddChoice(Resources.OnBoardingManageConanYes, Resources.OnBoardingManageConanYesSub);
            await DialogueBox.OpenAsync(choice);
            if(choice.Result < 0) throw new Exception("OnBoarding Failed");
            _setup.Config.ManageClient = choice.Result == 1;
            return await OnBoardingApplyConanManagement();
        }

        private async Task<bool> OnBoardingApplyConanManagement()
        {
            var clientDirectory = Path.GetFullPath(_setup.Config.ClientPath);
            if (!Tools.IsClientInstallValid(clientDirectory)) return false;
            var savedDir = Path.Combine(clientDirectory, Constants.FolderGameSave);
            
            if (!Directory.Exists(savedDir))
            {
                if (!await OnBoardingElevationRequest(clientDirectory, Resources.OnBoardingManageConanUac)) return false;
                if(_setup.Config.ManageClient)
                    Tools.SetupSymboliclink(savedDir, _appFiles.Client.GetPrimaryJunction());
                else
                {
                    Directory.CreateDirectory(savedDir);
                    if (!_appFiles.Client.ListProfiles().Any())
                        return true;
                    var saveName = await OnBoardingChooseClientSave();
                    await Tools.DeepCopyAsync(_appFiles.Client.GetFolder(saveName), savedDir, CancellationToken.None);
                }
                return true;
            }

            if (JunctionPoint.Exists(savedDir) && !_setup.Config.ManageClient)
            {
                if (!await OnBoardingElevationRequest(clientDirectory, Resources.OnBoardingManageConanUac)) return false;
                JunctionPoint.Delete(savedDir);
                Directory.CreateDirectory(savedDir);
                if (!_appFiles.Client.ListProfiles().Any())
                    return true;
                var saveName = await OnBoardingChooseClientSave();
                await Tools.DeepCopyAsync(_appFiles.Client.GetFolder(saveName), savedDir, CancellationToken.None);
                return true;
            }

            if (!JunctionPoint.Exists(savedDir) && _setup.Config.ManageClient)
            {
                if (!await OnBoardingElevationRequest(clientDirectory, Resources.OnBoardingManageConanUac)) return false;
                var saveName = await OnBoardingChooseClientSaveName();
                await Tools.DeepCopyAsync(savedDir, _appFiles.Client.GetFolder(saveName), CancellationToken.None);
                Directory.Delete(savedDir, true);
                Tools.SetupSymboliclink(savedDir, _appFiles.Client.GetPrimaryJunction());
                return true;
            }

            if (JunctionPoint.Exists(savedDir))
            {
                var path = JunctionPoint.GetTarget(savedDir);
                if (Path.GetFullPath(path) != Path.GetFullPath(_appFiles.Client.GetPrimaryJunction()))
                    JunctionPoint.Create(savedDir, _appFiles.Client.GetPrimaryJunction(), true);
            }

            return true;
        }

        private async Task<string> OnBoardingChooseClientSave()
        {
            var list = _appFiles.Client.ListProfiles().ToList();
            if (list.Count == 0)
                return await OnBoardingChooseClientSaveName();
            var choice = new OnBoardingListSelection(
                    Resources.OnBoardingGameSave, 
                    Resources.OnBoardingChooseGameSaveText, 
                    _appFiles.Client.ListProfiles().ToList())
                .SetSize<OnBoardingListSelection>(650, 200);
            await DialogueBox.OpenAsync(choice);
            if(string.IsNullOrEmpty(choice.SelectedElement)) throw new Exception("OnBoarding Failed");
            return choice.SelectedElement;
        }

        private async Task<string> OnBoardingChooseClientSaveName()
        {
            var choice = new OnBoardingNameSelection( Resources.OnBoardingGameSave, Resources.OnBoardingNewGameSave)
                .SetValidation(ValidateClientSaveName)
                .SetSize<OnBoardingNameSelection>(650, 200);
            await DialogueBox.OpenAsync(choice);
            if(string.IsNullOrEmpty(choice.Value)) throw new Exception("OnBoarding Failed");
            return choice.Value;
        }

        private Validation ValidateClientSaveName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new Validation(false, Resources.ErrorNameEmpty);
            if(_appFiles.Client.ListProfiles().Any(x => x == name))
                return new Validation(false, Resources.ErrorNameAlreadyTaken);
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return new Validation(false, Resources.ErrorNameInvalidCharacters);
            return new Validation(true, string.Empty);
        }
        
        private async Task<bool> OnBoardingElevationRequest(string path, string reason)
        {
            var canWriteInTrebuchet = Tools.IsDirectoryWritable(path);
            var isRoot = Tools.IsProcessElevated();
            if (!canWriteInTrebuchet)
            {
                if(isRoot) 
                    throw new Exception($"Can't write in {path}, permission denied");
                var uac = new OnBoardingBranch(Resources.UACDialog, Resources.UACDialogText + Environment.NewLine + reason)
                    .SetSize<OnBoardingBranch>(650, 250)
                    .AddChoice(Resources.UACDialog, Resources.OnBoardingUpgradeSub);
                await DialogueBox.OpenAsync(uac);
                if(uac.Result < 0) throw new Exception("Uac failed");
                Utils.Utils.RestartProcess(_setup.IsTestLive, true);
                return false;
            }

            return true;
        }
        
        #region Onboarding Upgrade

#pragma warning disable CS0612 // Type or member is obsolete
        private async Task<bool> OnBoardingCheckTrebuchet()
        {
            // Check old versions of trebuchet for upgrade path
            string configLive = "Live.Config.json";
            string configTestlive = "TestLive.Config.json";
            var trebuchetDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            configLive = Path.Combine(trebuchetDir, configLive);
            configTestlive = Path.Combine(trebuchetDir, configTestlive);

            if (File.Exists(configLive) || File.Exists(configTestlive))
            {
                var upgrade = new OnBoardingBranch(Resources.Upgrade, Resources.OnBoardingUpgrade)
                    .SetSize<OnBoardingBranch>(650, 250)
                    .AddChoice(Resources.Upgrade, Resources.OnBoardingUpgradeSub);
                await DialogueBox.OpenAsync(upgrade);
                if(upgrade.Result < 0) throw new Exception("OnBoardingUpgrade failed");

                if (!await OnBoardingElevationRequest(trebuchetDir, Resources.OnBoardingUpgradeUac)) return false;

                var progress = new OnBoardingProgress(Resources.Upgrade, Resources.OnBoardingUpgradeCopy)
                    .SetSize<OnBoardingProgress>(600, 250);
                DialogueBox.Open(progress);

                if (File.Exists(configLive))
                {
                    var configJson = await File.ReadAllTextAsync(configLive);
                    var configuration = JsonSerializer.Deserialize<Config>(configJson);
                    if (configuration is not null)
                    {
                        if (!await OnBoardingUpgradeTrebuchet(configuration.InstallPath, false, progress)) return false;
                        configuration.InstallPath = string.Empty;
                        configJson = JsonSerializer.Serialize(configuration);
                        await File.WriteAllTextAsync(AppConstants.GetConfigPath(false), configJson);
                    }
                    File.Delete(configLive);
                }

                if (File.Exists(configTestlive))
                {
                    var configJson = await File.ReadAllTextAsync(configTestlive);
                    var configuration = JsonSerializer.Deserialize<Config>(configJson);
                    if (configuration is not null)
                    {
                        if(!await OnBoardingUpgradeTrebuchet(configuration.InstallPath, true, progress)) return false;
                        configuration.InstallPath = string.Empty;
                        configJson = JsonSerializer.Serialize(configuration);
                        await File.WriteAllTextAsync(AppConstants.GetConfigPath(true), configJson);
                    }
                    File.Delete(configTestlive);
                }
                
                Utils.Utils.RestartProcess(_setup.IsTestLive, false);
                return false;
            }
            
            return true;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private async Task<bool> OnBoardingUpgradeTrebuchet(string installDir, bool testlive, IProgress<double> progress)
        {
            bool isElevated = Tools.IsProcessElevated();
            string appDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception("App is installed in an invalid directory");
            installDir = installDir.Replace("%APP_DIRECTORY%", appDir); 
            if(string.IsNullOrEmpty(installDir)) return true;
            if(!Directory.Exists(installDir)) return true;
            if (!await OnBoardingElevationRequest(installDir, Resources.OnBoardingUpgradeUac)) return false;
            
            progress.Report(0.0);
            var versionDir = testlive ? Constants.FolderTestLive : Constants.FolderLive;
            var workshopDir = Path.Combine(installDir, Constants.FolderWorkshop);
            if (Directory.Exists(workshopDir))
            {
                await Tools.DeepCopyAsync(workshopDir, _appFiles.Mods.GetWorkshopFolder(), CancellationToken.None, progress);
                if (isElevated)
                    Tools.SetEveryoneAccess(new DirectoryInfo(_appFiles.Mods.GetWorkshopFolder()));
                Directory.Delete(workshopDir, true);
            }
            
            progress.Report(0.0);
            var instanceDir = Path.Combine(installDir, versionDir, Constants.FolderServerInstances);
            if (Directory.Exists(instanceDir))
            {
                Tools.RemoveAllJunctions(instanceDir); 
                await Tools.DeepCopyAsync(instanceDir, _appFiles.Server.GetBaseInstancePath(testlive), CancellationToken.None, progress);
                if(isElevated)
                    Tools.SetEveryoneAccess(new DirectoryInfo(_appFiles.Server.GetBaseInstancePath(testlive)));
                Directory.Delete(instanceDir, true);
            }
            progress.Report(0.0);
            
            var dataDir = Path.Combine(installDir, versionDir);
            if (Directory.Exists(dataDir))
            {
                await Tools.DeepCopyAsync(dataDir, Path.Combine(AppFiles.GetDataDirectory().FullName, versionDir), CancellationToken.None, progress);
                if(isElevated)
                    Tools.SetEveryoneAccess(new DirectoryInfo(Path.Combine(AppFiles.GetDataDirectory().FullName, versionDir)));
                Directory.Delete(dataDir, true);
            }
            
            progress.Report(0.0);
            var logsDir = Path.Combine(installDir, "Logs");
            if(Directory.Exists(logsDir))
                Directory.Delete(logsDir, true);
            
            progress.Report(1.0);
            return true;
        }
        
        #endregion

    }
}