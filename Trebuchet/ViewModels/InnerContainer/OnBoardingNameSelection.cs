using System;
using System.Collections.Generic;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingNameSelection : ValidatedInputDialogue<string>
{
    public OnBoardingNameSelection(string title, string description) : base(title, description)
    {
    }

    protected override string? ProcessValue(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}