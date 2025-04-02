using System;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class ValidatedInputDialogue<T> : TitledDialogue
{
    protected Func<T?, Validation> _validation;
    protected T? _value;
    protected bool _isValid = true;
    protected string _errorMessage = string.Empty;
    
    public ValidatedInputDialogue(string title, string description) : base(title, description)
    {
        _validation = (_) => Validation.Valid;
        ConfirmCommand = new SimpleCommand().Subscribe(Close);
        CancelCommand.Clear().Subscribe(() =>
        {
            _value = default(T);
            Close();
        });
    }
    
    public SimpleCommand ConfirmCommand { get; }
    
    public T? Value
    {
        get => _value;
        set
        {
            if (SetField(ref _value, ProcessValue(value)))
            {
                var result = _validation.Invoke(value);
                IsValid = result.IsValid;
                ErrorMessage = result.ErrorMessage;
            }
        }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        protected set => SetField(ref _errorMessage, value);
    }

    public bool IsValid
    {
        get => _isValid;
        protected set => SetField(ref _isValid, value);
    } 
    
    public ValidatedInputDialogue<T> SetValidation(Func<T?, Validation> validation)
    {
        _validation = validation;
        var result = _validation.Invoke(_value);
        IsValid = result.IsValid;
        ErrorMessage = result.ErrorMessage;
        return this;
    }

    protected virtual T? ProcessValue(T? value)
    {
        return value;
    }
}