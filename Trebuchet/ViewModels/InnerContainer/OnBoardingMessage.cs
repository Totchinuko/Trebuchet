using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingMessage : TitledDialogue<OnBoardingMessage>
{

    public OnBoardingMessage(string title, string description) : base(title, description)
    {
    }
}