using System;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingNameSelection : InnerPopup
{
    private readonly Func<string, Validation> _validation;
    private string _selectedName = string.Empty;
    private bool _isValid;
    private string _errorMessage;

    public OnBoardingNameSelection(string title, string description, Func<string, Validation> validation) : base()
    {
        _validation = validation;
        Title = title;
        Description = description;
        var result = _validation(string.Empty);
        _isValid = result.isValid;
        _errorMessage = result.errorMessage;
        ConfirmCommand = new SimpleCommand().Subscribe(Close);
    }
    
    public string Title { get; }
    public string Description { get; }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public SimpleCommand ConfirmCommand { get; }

    public bool IsValid
    {
        get => _isValid;
        set => SetField(ref _isValid, value);
    } 
    
    public string SelectedName
    {
        get => _selectedName;
        set
        {
            if (SetField(ref _selectedName, value))
            {
                var result = _validation(value);
                IsValid = result.isValid;
                ErrorMessage = result.errorMessage;
            }
        }
    }
}