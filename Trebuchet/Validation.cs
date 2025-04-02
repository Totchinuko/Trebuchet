using Trebuchet.Assets;

namespace Trebuchet;

public class Validation(bool isValid, string errorMessage)
{
    public bool IsValid { get; } = isValid;
    public string ErrorMessage { get; } = errorMessage;
    public Validation(bool isValid) : this(isValid, string.Empty)
    {
    }

    public static Validation Valid => new Validation(true);

    public static Validation Invalid(string errorMessage) => new Validation(false, errorMessage);
}