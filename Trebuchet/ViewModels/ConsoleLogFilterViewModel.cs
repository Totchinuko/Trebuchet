using System.Reactive;
using ReactiveUI;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class ConsoleLogFilterViewModel : ReactiveObject
{
    public ConsoleLogFilterViewModel(UIConfig config, int instance, ConsoleLogSource logSource, string name, string icon)
    {
        LogSource = logSource;
        Name = name;
        Icon = icon;
        _isDisplayed = config.GetInstanceFilter(instance, logSource);
        Toggle = ReactiveCommand.Create(() =>
        {
            IsDisplayed = !IsDisplayed;
            config.SetInstanceFilter(instance, logSource,IsDisplayed);
            config.SaveFile();
        });
    }
    
    private bool _isDisplayed;
    
    public ReactiveCommand<Unit,Unit> Toggle { get; }
    
    public ConsoleLogSource LogSource { get; }
    public string Name { get; }
    public string Icon { get; }

    public bool IsDisplayed
    {
        get => _isDisplayed;
        set => this.RaiseAndSetIfChanged(ref _isDisplayed, value);
    }
}