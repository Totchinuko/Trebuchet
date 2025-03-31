using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Trebuchet.Assets;
using Trebuchet.Messages;
using Trebuchet.Utils;
using Trebuchet.ViewModels.Panels;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public sealed class TrebuchetApp : INotifyPropertyChanged, ITinyRecipient<PanelActivateMessage>
    {
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly Launcher _launcher;
        private readonly Steam _steam;
        private Panel _activePanel;
        private List<Panel> _panels;
        private DispatcherTimer _timer;

        public TrebuchetApp(
            AppSetup setup,
            AppFiles appFiles,
            Launcher launcher, 
            Steam steam, 
            SteamWidget steamWidget,
            InnerContainer.InnerContainer innerContainer,
            IEnumerable<Panel> panels)
        {
            _setup = setup;
            _appFiles = appFiles;
            _launcher = launcher;
            _steam = steam;
            _panels = panels.ToList();
            SteamWidget = steamWidget;
            InnerContainer = innerContainer;

            TinyMessengerHub.Default.Subscribe(this);

            OrderPanels(_panels);
            _activePanel = BottomPanels.First(x => x.CanExecute(null));
            _activePanel.Active = true;
            
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimerTick);
            _timer.Start();
            
            OnBoardingActions();
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {
            _timer.Stop();
            await _launcher.Tick();
            foreach (var panel in _panels)
                panel.Tick();
            _timer.Start();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static string AppTitle => $"Tot ! Trebuchet {TrebuchetUtils.Utils.GetFileVersion()}";
        public Panel ActivePanel
        {
            get => _activePanel;
            set
            {
                if(_activePanel == value) return;
                _activePanel.Active = false;
                _activePanel = value;
                _activePanel.Active = true;
                _activePanel.PanelDisplayed();
                OnPropertyChanged(nameof(ActivePanel));
            }
        }

        public ObservableCollection<Panel> TopPanels { get; } = [];
        public ObservableCollection<Panel> BottomPanels { get; } = [];

        public SteamWidget SteamWidget { get; }
        
        public InnerContainer.InnerContainer InnerContainer { get; }

        public async void OnWindowShow()
        {
            _panels.ForEach(x => x.OnWindowShow());
            await _steam.Connect();
        }

        public void Receive(PanelActivateMessage message)
        {
            ActivePanel = message.Panel;
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

        private void OrderPanels(List<Panel> panels)
        {
            foreach (var panel in panels)
            {
                if(panel.BottomPosition)
                    BottomPanels.Add(panel);
                else
                    TopPanels.Add(panel);
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private async void OnBoardingActions()
        {
            _appFiles.SetupFolders();
            if (!await OnBoardingCheckTrebuchet()) return;
        }

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
                await InnerContainer.OpenAsync(upgrade);
                if(upgrade.Result < 0) throw new Exception("OnBoardingUpgrade failed");

                if (!await OnBoardingElevationRequest(trebuchetDir, Resources.OnBoardingUpgradeUac)) return false;

                var progress = new OnBoardingProgress(Resources.Upgrade, Resources.OnBoardingUpgradeCopy);
                InnerContainer.Open(progress);

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
                await Tools.DeepCopyAsync(dataDir, Path.Combine(AppFiles.GetDataFolder(), versionDir), CancellationToken.None, progress);
                if(isElevated)
                    Tools.SetEveryoneAccess(new DirectoryInfo(Path.Combine(AppFiles.GetDataFolder(), versionDir)));
                Directory.Delete(dataDir, true);
            }
            
            progress.Report(0.0);
            var logsDir = Path.Combine(installDir, "Logs");
            if(Directory.Exists(logsDir))
                Directory.Delete(logsDir, true);
            
            progress.Report(1.0);
            return true;
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
                await InnerContainer.OpenAsync(uac);
                if(uac.Result < 0) throw new Exception("Uac failed");
                Utils.Utils.RestartProcess(_setup.IsTestLive, true);
                return false;
            }

            return true;
        }
    }
}