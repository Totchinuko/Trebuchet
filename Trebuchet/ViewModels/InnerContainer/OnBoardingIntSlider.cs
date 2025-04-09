namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingIntSlider : ValidatedInputDialogue<int, OnBoardingIntSlider>
{
    public OnBoardingIntSlider(string title, string description, int minimum, int maximum) : base(title, description)
    {
        Minimum = minimum;
        Maximum = maximum;
    }
    
    public int Minimum { get; }
    public int Maximum { get; }
}