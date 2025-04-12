using System;
using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingChoice
{
    public OnBoardingChoice(string title, string description, int index, Action<int> command)
    {
        Title = title;
        Description = description;
        Index = index;
        Command = ReactiveCommand.Create(() => command(Index));
    }

    public string Title { get; }
    public string Description { get; }
    public int Index { get; }
    public ReactiveCommand<Unit, Unit> Command { get; }
}