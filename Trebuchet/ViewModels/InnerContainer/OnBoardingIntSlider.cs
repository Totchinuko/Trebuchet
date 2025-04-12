namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingIntSlider(string title, string description, int minimum, int maximum)
    : ValidatedInputDialogue<int, OnBoardingIntSlider>(title, description)
{
    public int Minimum { get; } = minimum;
    public int Maximum { get; } = maximum;
}