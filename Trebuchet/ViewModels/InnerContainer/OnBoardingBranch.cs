using System.Collections.ObjectModel;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingBranch(string title, string description) : InnerPopup("OnBoardingBranch")
{
    public ObservableCollection<OnBoardingChoice> Choices { get; } = [];
    public string Title { get; } = title;
    public string Description { get; } = description;
    public int Result { get; private set; } = -1;

    public OnBoardingBranch AddChoice(string title, string description)
    {
        Choices.Add(new OnBoardingChoice(title, description, Choices.Count, OnChoiceMade));
        return this;
    }

    private void OnChoiceMade(int index)
    {
        Result = index;
        Close();
    }
}