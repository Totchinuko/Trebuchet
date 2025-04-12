using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingError : TitledDialogue<OnBoardingError>
{
    private readonly ObservableAsPropertyHelper<string> _label;
    private readonly bool _exit;
    
    public OnBoardingError(string title, string description, bool exit) : base(title, description)
    {
        _label = this.WhenAnyValue(x => x.Exit)
            .Select(x => x ? Resources.Exit : Resources.Close)
            .ToProperty(this, x => x.Label);
        Exit = exit;
        CancelCommand.Subscribe((_) =>
        {
            if (Exit)
                Utils.Utils.ShutdownDesktopProcess();
        });
    }
    
    public bool Exit
    {
        get => _exit;
        init => this.RaiseAndSetIfChanged(ref _exit, value);
    }

    public string Label => _label.Value;
}

public static class OnBoardingErrorEx
{
    public static async Task OpenErrorAsync(this DialogueBox box, string title, string description)
    {
        var error = new OnBoardingError(title, description, false);
        await box.OpenAsync(error);
    } 
    
    public static async Task OpenErrorAndExitAsync(this DialogueBox box, string title, string description)
    {
        var error = new OnBoardingError(title, description, true);
        await box.OpenAsync(error);
    } 
    
    public static async Task OpenErrorAsync(this DialogueBox box, string description)
    {
        var error = new OnBoardingError(Resources.Error, description, false);
        await box.OpenAsync(error);
    } 
    
    public static async Task OpenErrorAndExitAsync(this DialogueBox box, string description)
    {
        var error = new OnBoardingError(Resources.Error, description, true);
        await box.OpenAsync(error);
    } 
}