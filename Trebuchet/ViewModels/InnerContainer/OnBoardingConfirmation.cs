using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingConfirmation : TitledDialogue<OnBoardingConfirmation>
{

    public OnBoardingConfirmation(string title, string description) : base(title, description)
    {
        CanCancel = true;
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            Result = true;
            Close();
        });
    }
    
    public bool Result { get; private set; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

}