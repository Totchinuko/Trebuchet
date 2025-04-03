using System;
using System.Collections.Generic;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingConfirmation : TitledDialogue
{

    public OnBoardingConfirmation(string title, string description) : base(title, description)
    {
        ConfirmCommand.Subscribe(() =>
        {
            Result = true;
            Close();
        });
    }
    
    public bool Result { get; private set; }

    public SimpleCommand ConfirmCommand { get; } = new();

}