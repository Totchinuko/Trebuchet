using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingNameSelection(string title, string description)
    : ValidatedInputDialogue<string, OnBoardingNameSelection>(title, description)
{
    protected override string ProcessValue(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private string _watermark = string.Empty;

    public string Watermark
    {
        get => _watermark;
        set => this.RaiseAndSetIfChanged(ref _watermark, value);
    }
}