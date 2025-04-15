using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Services;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.SettingFields;

public class AppDataDirectoryField : DescriptiveElement<AppDataDirectoryField>
{
    public AppDataDirectoryField(OnBoarding onBoarding, AppSetup setup)
    {
        _onBoarding = onBoarding;
        _setup = setup;

        var isCustomized = this.WhenAnyValue(x => x.Customized);
        Remove = ReactiveCommand.Create(RemoveDataDirectoryCustomization, isCustomized);
        Change = ReactiveCommand.CreateFromTask(ChangeDataDirectory);

        _pathDisplay = this.WhenAnyValue(x => x.DataDirectory)
            .Select(x => string.IsNullOrEmpty(x) ? AppSetup.GetCommonAppDataDirectoryDefault().FullName : x)
            .ToProperty(this, x => x.PathDisplay);

        Customized = !string.IsNullOrEmpty(setup.Config.DataDirectory);
        DataDirectory = setup.Config.DataDirectory;
    }
    
    private readonly OnBoarding _onBoarding;
    private readonly AppSetup _setup;
    private bool _customized;
    private string _dataDirectory = string.Empty;
    private ObservableAsPropertyHelper<string> _pathDisplay;

    public ReactiveCommand<Unit,Unit> Remove { get; }
    public ReactiveCommand<Unit,Unit> Change { get; }

    public bool Customized
    {
        get => _customized;
        set => this.RaiseAndSetIfChanged(ref _customized, value);
    }
    
    public string DataDirectory
    {
        get => _dataDirectory;
        set => this.RaiseAndSetIfChanged(ref _dataDirectory, value);
    }

    public string PathDisplay => _pathDisplay.Value;

    public AppDataDirectoryField WhenFieldChanged(ReactiveCommand<Unit, Unit> command)
    {
        this.WhenAnyValue(x => x.DataDirectory)
            .Select((_) => Unit.Default)
            .InvokeCommand(command);
        return this;
    }

    private async Task ChangeDataDirectory()
    {
        try
        {
            var success = await _onBoarding.OnBoardingChangeDataDirectory();
            if (success)
            {
                _setup.Config.SaveFile();
                Customized = true;
                DataDirectory = _setup.Config.DataDirectory;
                Utils.Utils.RestartProcess(_setup);
            }
        }
        catch(OperationCanceledException) {}
    }

    private void RemoveDataDirectoryCustomization()
    {
        try
        {
            _setup.Config.DataDirectory = string.Empty;
            _setup.Config.SaveFile();
            DataDirectory = string.Empty;
            Customized = false;
            Utils.Utils.RestartProcess(_setup);
        }
        catch(OperationCanceledException) {}
    }
}