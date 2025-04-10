using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Trebuchet.Assets;
using Trebuchet.Services.Language;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.Services;

public class OnBoarding(
    AppFiles appFiles, 
    AppSetup setup, 
    DialogueBox dialogueBox,
    UIConfig uiConfig,
    ILanguageManager langManager,
    Steam steam, 
    SteamAPI steamApi)
{
    public async Task<bool> OnBoardingRemoveUnusedMods()
    {
        var count = steamApi.CountUnusedMods();
        if (count <= 0)
        {
            var message = new OnBoardingMessage(Resources.OnBoardingNoUnusedMods, Resources.OnBoardingNoUnusedModsSub);
            await dialogueBox.OpenAsync(message);
            return true;
        }

        var confirm = new OnBoardingConfirmation(Resources.OnBoardingModTrimConfirm,
            string.Format(Resources.OnBoardingModTrimConfirmSub, count));
        await dialogueBox.OpenAsync(confirm);
        if (!confirm.Result) throw new OperationCanceledException(@"OnBoarding was cancelled");
        var progress = new OnBoardingProgress(Resources.OnBoardingModTrimConfirm, string.Empty);
        progress.Report(0);
        dialogueBox.Open(progress);
        await steamApi.RemoveUnusedMods();
        dialogueBox.Close();
        return true;
    }

    public async Task<bool> OnBoardingLanguageChoice()
    {
        if (!string.IsNullOrEmpty(uiConfig.UICulture)) return true;
        var choice = new OnBoardingLanguage(Resources.OnBoardingLanguageChange, string.Empty, 
            langManager.AllLanguages.ToList(), langManager.DefaultLanguage);
        await dialogueBox.OpenAsync(choice);
        if(choice.Value is null) throw new OperationCanceledException(@"OnBoarding was cancelled");
        uiConfig.UICulture = choice.Value.Code;
        uiConfig.SaveFile();
        Utils.Utils.RestartProcess(setup);
        return false;
    }
    
    public async Task<bool> OnBoardingUsageChoice()
    {
        var choice = new OnBoardingBranch(Resources.OnBoardingUsageChoice, Resources.OnBoardingUsageChoiceText)
            .SetSize<OnBoardingBranch>(750, 400)
            .AddChoice(Resources.OnBoardingUsageChoicePlayer, Resources.OnBoardingUsageChoicePlayerSub)
            .AddChoice(Resources.OnBoardingUsageChoiceServer, Resources.OnBoardingUsageChoiceServerSub)
            .AddChoice(Resources.OnBoardingUsageChoiceModder, Resources.OnBoardingUsageChoiceModderSub);
        await dialogueBox.OpenAsync(choice);
        switch (choice.Result)
        {
            case 0: // Play Conan
                setup.Config.AutoUpdateStatus = AutoUpdateStatus.Never;
                setup.Config.ServerInstanceCount = 0;
                return await OnBoardingFindConanExile();
            case 1: //Server Admin
                setup.Config.AutoUpdateStatus = AutoUpdateStatus.CheckForUpdates;
                setup.Config.ServerInstanceCount = 1;
                return await OnBoardingServerInstanceSelection();
            case 2: // Modding
                setup.Config.AutoUpdateStatus = AutoUpdateStatus.Never;
                setup.Config.ServerInstanceCount = 1;
                if(!await OnBoardingFindConanExile()) return false;
                return await OnBoardingServerDownload();
            default:
                throw new OperationCanceledException(@"OnBoarding was cancelled");
        }
    }

    public async Task<bool> OnBoardingServerInstanceSelection()
    {
        var choice = new OnBoardingIntSlider(
            Resources.OnBoardingServerInstanceCount, 
            Resources.OnBoardingServerInstanceCountSub,
            1, 6);
        choice.Value = setup.Config.ServerInstanceCount;
        await dialogueBox.OpenAsync(choice);
        if(choice.Value == 0) throw new OperationCanceledException(@"OnBoarding was cancelled");
        setup.Config.ServerInstanceCount = choice.Value;
        return await OnBoardingServerDownload();
    }

    public async Task<bool> OnBoardingServerDownload()
    {
        var progress = new OnBoardingProgress(Resources.UpdateServersLabel, string.Empty)
            .SetSize<OnBoardingProgress>(600, 250);
        dialogueBox.Open(progress);
        steam.SetTemporaryProgress(progress);
        if(!steam.IsConnected)
            await steam.Connect();
        var cts = new CancellationTokenSource();
        await steam.UpdateServerInstances(cts);
        progress.Progress = 1.0;
        steam.RestoreProgress();
        progress.Close();
        return true;
    }

    public async Task<bool> OnBoardingFindConanExile(bool force = false)
    {
        if (Tools.IsClientInstallValid(setup.Config.ClientPath) && !force)
            return await OnBoardingApplyConanManagement();
        
        var finder = new OnBoardingDirectory(Resources.OnBoardingLocateConan, Resources.OnBoardingLocateConanText, setup.Config.ClientPath)
            .SetValidation(ValidateConanExileLocation)
            .SetSize<OnBoardingDirectory>(600, 200);
        await dialogueBox.OpenAsync(finder);
        if(finder.Value is null) throw new OperationCanceledException(@"OnBoarding was cancelled");
        setup.Config.ClientPath = finder.Value;
        return await OnBoardingAllowConanManagement();
    }

    public Validation ValidateConanExileLocation(string? path)
    {
        if(string.IsNullOrEmpty(path))
            return Validation.Invalid(Resources.ErrorValueEmpty);
        if (!Tools.IsClientInstallValid(path))
            return new Validation(false, Resources.OnBoardingLocateConanError);
        return new Validation(true, string.Empty);
    }

    public async Task<bool> OnBoardingAllowConanManagement()
    {
        var choice = new OnBoardingBranch(Resources.OnBoardingManageConan, Resources.OnBoardingManageConanText)
            .SetSize<OnBoardingBranch>(650, 300)
            .AddChoice(Resources.OnBoardingManageConanNo, Resources.OnBoardingManageConanNoSub)
            .AddChoice(Resources.OnBoardingManageConanYes, Resources.OnBoardingManageConanYesSub);
        await dialogueBox.OpenAsync(choice);
        if(choice.Result < 0) throw new OperationCanceledException(@"OnBoarding was cancelled");
        setup.Config.ManageClient = choice.Result == 1;
        return await OnBoardingApplyConanManagement();
    }

    public async Task<bool> OnBoardingApplyConanManagement()
    {
        var clientDirectory = Path.GetFullPath(setup.Config.ClientPath);
        if (!Tools.IsClientInstallValid(clientDirectory)) return false;
        var savedDir = Path.Combine(clientDirectory, Constants.FolderGameSave);
        
        if (!Directory.Exists(savedDir))
        {
            if (!await OnBoardingElevationRequest(clientDirectory, Resources.OnBoardingManageConanUac)) return false;
            if(setup.Config.ManageClient)
                Tools.SetupSymboliclink(savedDir, appFiles.Client.GetPrimaryJunction());
            else
            {
                if (!appFiles.Client.ListProfiles().Any())
                    return true;
                var saveName = await OnBoardingChooseClientSave();
                Directory.CreateDirectory(savedDir);
                await Tools.DeepCopyAsync(appFiles.Client.GetFolder(saveName), savedDir, CancellationToken.None);
            }
            return true;
        }

        if (JunctionPoint.Exists(savedDir) && !setup.Config.ManageClient)
        {
            if (!await OnBoardingElevationRequest(clientDirectory, Resources.OnBoardingManageConanUac)) return false;
            if (!appFiles.Client.ListProfiles().Any())
                return true;
            var saveName = await OnBoardingChooseClientSave();
            JunctionPoint.Delete(savedDir);
            Directory.CreateDirectory(savedDir);
            await Tools.DeepCopyAsync(appFiles.Client.GetFolder(saveName), savedDir, CancellationToken.None);
            return true;
        }

        if (!JunctionPoint.Exists(savedDir) && setup.Config.ManageClient)
        {
            if (!await OnBoardingElevationRequest(clientDirectory, Resources.OnBoardingManageConanUac)) return false;
            var saveName = await OnBoardingChooseClientSaveName();
            await Tools.DeepCopyAsync(savedDir, appFiles.Client.GetFolder(saveName), CancellationToken.None);
            await OnBoardingSafeIO(() => Directory.Delete(savedDir, true),savedDir);
            Tools.SetupSymboliclink(savedDir, appFiles.Client.GetPrimaryJunction());
            return true;
        }

        if (JunctionPoint.Exists(savedDir))
        {
            var path = JunctionPoint.GetTarget(savedDir);
            if (Path.GetFullPath(path) != Path.GetFullPath(appFiles.Client.GetPrimaryJunction()))
                JunctionPoint.Create(savedDir, appFiles.Client.GetPrimaryJunction(), true);
        }

        return true;
    }

    public async Task<string> OnBoardingChooseClientSave()
    {
        var list = appFiles.Client.ListProfiles().ToList();
        if (list.Count == 0)
            return await OnBoardingChooseClientSaveName();
        var choice = new OnBoardingListSelection(
                Resources.OnBoardingGameSave, 
                Resources.OnBoardingChooseGameSaveText, 
                appFiles.Client.ListProfiles().ToList())
            .SetSize<OnBoardingListSelection>(650, 200);
        await dialogueBox.OpenAsync(choice);
        if(string.IsNullOrEmpty(choice.Value)) throw new OperationCanceledException(@"OnBoarding was cancelled");
        return choice.Value;
    }

    public async Task<string> OnBoardingChooseClientSaveName()
    {
        var choice = new OnBoardingNameSelection( Resources.OnBoardingGameSave, Resources.OnBoardingNewGameSave)
            .SetValidation(ValidateClientSaveName)
            .SetSize<OnBoardingNameSelection>(650, 200);
        await dialogueBox.OpenAsync(choice);
        if(string.IsNullOrEmpty(choice.Value)) throw new OperationCanceledException(@"OnBoarding was cancelled");
        return choice.Value;
    }

    public Validation ValidateClientSaveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return new Validation(false, Resources.ErrorNameEmpty);
        if(appFiles.Client.ListProfiles().Any(x => x == name))
            return new Validation(false, Resources.ErrorNameAlreadyTaken);
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return new Validation(false, Resources.ErrorNameInvalidCharacters);
        return new Validation(true, string.Empty);
    }
    
    public async Task<bool> OnBoardingElevationRequest(string path, string reason)
    {
        var canWriteInTrebuchet = Tools.IsDirectoryWritable(path);
        var isRoot = Tools.IsProcessElevated();
        if (!canWriteInTrebuchet)
        {
            if(isRoot) 
                throw new IOException(@$"Can't write in {path}, permission denied");
            var uac = new OnBoardingBranch(Resources.UACDialog, Resources.UACDialogText + Environment.NewLine + reason)
                .SetSize<OnBoardingBranch>(650, 250)
                .AddChoice(Resources.UACDialog, Resources.OnBoardingUpgradeSub);
            await dialogueBox.OpenAsync(uac);
            if(uac.Result < 0) throw new OperationCanceledException(@"OnBoarding was cancelled");
            Utils.Utils.RestartProcess(setup, true);
            return false;
        }

        return true;
    }

    public async Task<bool> OnBoardingSafeIO(Action ioAction, string file)
    {
        var currentPopup = dialogueBox.Popup;
        dialogueBox.Close();
        while (true)
        {
            try
            {
                ioAction.Invoke();
                break;
            }
            catch (IOException ex)
            {
                if (TrebuchetUtils.Utils.IsFileLocked(ex))
                {
                    if (!dialogueBox.Active)
                    {
                        var message = new OnBoardingProgress(
                            Resources.OnBoardingProcessLock,
                            string.Format(Resources.OnBoardingProcessLockSub, file))
                            .SetSize<OnBoardingProgress>(600, 250);
                        message.Report(0);
                        dialogueBox.Open(message);
                    }
                }
                else
                    throw;
            }
            await Task.Delay(1000);
        }
        if(dialogueBox.Active)
            dialogueBox.Close();
        if(currentPopup is not null)
            dialogueBox.Open(currentPopup);
        return true;
    }
    
    #region Onboarding Upgrade

#pragma warning disable CS0612 // Type or member is obsolete
    public async Task<bool> OnBoardingCheckTrebuchet()
    {
        // Check old versions of trebuchet for upgrade path
        string configLive = @"Live.Config.json";
        string configTestlive = @"TestLive.Config.json";
        var trebuchetDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        configLive = Path.Combine(trebuchetDir, configLive);
        configTestlive = Path.Combine(trebuchetDir, configTestlive);

        if (File.Exists(configLive) || File.Exists(configTestlive))
        {
            var upgrade = new OnBoardingBranch(Resources.Upgrade, Resources.OnBoardingUpgrade)
                .SetSize<OnBoardingBranch>(650, 250)
                .AddChoice(Resources.Upgrade, Resources.OnBoardingUpgradeSub);
            await dialogueBox.OpenAsync(upgrade);
            if(upgrade.Result < 0) throw new OperationCanceledException(@"OnBoarding was cancelled");

            if (!await OnBoardingElevationRequest(trebuchetDir, Resources.OnBoardingUpgradeUac)) return false;

            var progress = new OnBoardingProgress(Resources.Upgrade, Resources.OnBoardingUpgradeCopy)
                .SetSize<OnBoardingProgress>(600, 250);
            dialogueBox.Open(progress);

            if (File.Exists(configLive))
            {
                var configJson = await File.ReadAllTextAsync(configLive);
                var configuration = JsonSerializer.Deserialize<Config>(configJson);
                if (configuration is not null)
                {
                    if (!await OnBoardingUpgradeTrebuchet(configuration.InstallPath, false, progress)) return false;
                    configuration.InstallPath = string.Empty;
                    configuration.ManageClient = true;
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
                    configuration.ManageClient = true;
                    configJson = JsonSerializer.Serialize(configuration);
                    await File.WriteAllTextAsync(AppConstants.GetConfigPath(true), configJson);
                }
                File.Delete(configTestlive);
            }
            
            Utils.Utils.RestartProcess(setup);
            return false;
        }
        
        return true;
    }
#pragma warning restore CS0612 // Type or member is obsolete

    public async Task<bool> OnBoardingUpgradeTrebuchet(string installDir, bool testlive, IProgress<double> progress)
    {
        bool isElevated = Tools.IsProcessElevated();
        string appDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception(@"App is installed in an invalid directory");
        installDir = installDir.Replace(@"%APP_DIRECTORY%", appDir); 
        if(string.IsNullOrEmpty(installDir)) return true;
        if(!Directory.Exists(installDir)) return true;
        if (!await OnBoardingElevationRequest(installDir, Resources.OnBoardingUpgradeUac)) return false;
        
        progress.Report(0.0);
        var versionDir = testlive ? Constants.FolderTestLive : Constants.FolderLive;
        var workshopDir = Path.Combine(installDir, Constants.FolderWorkshop);
        if (Directory.Exists(workshopDir))
        {
            await Tools.DeepCopyAsync(workshopDir, appFiles.Mods.GetWorkshopFolder(), CancellationToken.None, progress);
            if (isElevated)
                Tools.SetEveryoneAccess(new DirectoryInfo(appFiles.Mods.GetWorkshopFolder()));
            await OnBoardingSafeIO(() => Directory.Delete(workshopDir, true), workshopDir);
        }
        
        progress.Report(0.0);
        var instanceDir = Path.Combine(installDir, versionDir, Constants.FolderServerInstances);
        if (Directory.Exists(instanceDir))
        {
            Tools.RemoveAllJunctions(instanceDir); 
            await Tools.DeepCopyAsync(instanceDir, appFiles.Server.GetBaseInstancePath(testlive), CancellationToken.None, progress);
            if(isElevated)
                Tools.SetEveryoneAccess(new DirectoryInfo(appFiles.Server.GetBaseInstancePath(testlive)));
            await OnBoardingSafeIO(() => Directory.Delete(instanceDir, true), instanceDir);
        }
        progress.Report(0.0);
        
        var dataDir = Path.Combine(installDir, versionDir);
        if (Directory.Exists(dataDir))
        {
            await Tools.DeepCopyAsync(dataDir, Path.Combine(AppFiles.GetDataDirectory().FullName, versionDir), CancellationToken.None, progress);
            if(isElevated)
                Tools.SetEveryoneAccess(new DirectoryInfo(Path.Combine(AppFiles.GetDataDirectory().FullName, versionDir)));
            await OnBoardingSafeIO(() => Directory.Delete(dataDir, true),dataDir);
        }
        
        progress.Report(0.0);
        var logsDir = Path.Combine(installDir, @"Logs");
        if(Directory.Exists(logsDir))
            await OnBoardingSafeIO(() => Directory.Delete(logsDir, true), logsDir);
        
        progress.Report(1.0);
        return true;
    }
    
    #endregion

}