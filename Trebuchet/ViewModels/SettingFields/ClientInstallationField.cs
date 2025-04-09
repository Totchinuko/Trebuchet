using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.SettingFields;

public class ClientInstallationField : DescriptiveElement<ClientInstallationField>
{
    public ClientInstallationField(OnBoarding onBoarding, AppSetup setup)
    {
        _onBoarding = onBoarding;
        _setup = setup;

        var isInstalled = this.WhenAnyValue(x => x.Installed);
        _installLabel = this.WhenAnyValue(x => x.Installed)
            .Select(x => x ? Resources.Change : Resources.Find)
            .ToProperty(this, x => x.InstallLabel);

        _manageFileLabel = this.WhenAnyValue(x => x.ManageFiles)
            .Select(x => x ? Resources.Yes : Resources.No)
            .ToProperty(this, x => x.ManageFileLabel);
        
        Remove = ReactiveCommand.Create(() =>
        {
            setup.Config.ClientPath = string.Empty;
            setup.Config.ManageClient = false;
            setup.Config.SaveFile();
            InstallPath = string.Empty;
            ManageFiles = false;
            Installed = false;
        }, isInstalled);
        
        Install = ReactiveCommand.CreateFromTask(InstallClientPath);

        Installed = Tools.IsClientInstallValid(setup.Config);
        InstallPath = setup.Config.ClientPath;
        ManageFiles = setup.Config.ManageClient;
    }
    
    private readonly OnBoarding _onBoarding;
    private readonly AppSetup _setup;
    private bool _installed;
    private bool _manageFiles;
    private string _installPath = string.Empty;
    private readonly ObservableAsPropertyHelper<string> _installLabel;
    private readonly ObservableAsPropertyHelper<string> _manageFileLabel;

    public ReactiveCommand<Unit,Unit> Remove { get; }
    public ReactiveCommand<Unit,Unit> Install { get; }

    public bool Installed
    {
        get => _installed;
        set => this.RaiseAndSetIfChanged(ref _installed, value);
    }
    
    public bool ManageFiles
    {
        get => _manageFiles;
        set => this.RaiseAndSetIfChanged(ref _manageFiles, value);
    }

    public string InstallPath
    {
        get => _installPath;
        set => this.RaiseAndSetIfChanged(ref _installPath, value);
    }

    public string InstallLabel => _installLabel.Value;
    public string ManageFileLabel => _manageFileLabel.Value;

    public ClientInstallationField WhenFieldChanged(ReactiveCommand<Unit, Unit> command)
    {
        this.WhenAnyValue(x => x.InstallPath, x => x.ManageFiles)
            .Select((_,_) => Unit.Default)
            .InvokeCommand(command);
        return this;
    }

    private async Task InstallClientPath()
    {
        try
        {
            var success = await _onBoarding.OnBoardingFindConanExile(force:true);
            if (success)
            {
                _setup.Config.SaveFile();
                Installed = true;
                InstallPath = _setup.Config.ClientPath;
                ManageFiles = _setup.Config.ManageClient;
            }
        }
        catch(OperationCanceledException) {}
    }
}