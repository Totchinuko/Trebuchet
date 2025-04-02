using System;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingChoice
{
    public OnBoardingChoice(string title, string description, int index, Action<int> command)
    {
        Title = title;
        Description = description;
        Index = index;
        Command = new SimpleCommand().Subscribe(() => command(Index));
    }

    public string Title { get; }
    public string Description { get; }
    public int Index { get; }
    public ICommand Command { get; }
}