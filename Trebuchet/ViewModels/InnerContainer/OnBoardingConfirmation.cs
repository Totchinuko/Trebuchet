using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingConfirmation : TitledDialogue
{

    public OnBoardingConfirmation(string title, string description) : base(title, description)
    {
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            Result = true;
            Close();
        });
    }
    
    public bool Result { get; private set; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

}