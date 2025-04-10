namespace Trebuchet.Services.Language;

public class LanguagesConfiguration(string[] availableLocales)
{
    public string[] AvailableLocales { get; set; } = availableLocales;
}