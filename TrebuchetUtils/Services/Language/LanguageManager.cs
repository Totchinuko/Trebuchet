using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace TrebuchetUtils.Services.Language;

public class LanguageManager : ILanguageManager
{
    private readonly LanguagesConfiguration _configuration;
    private readonly Lazy<Dictionary<string, LanguageModel>> _availableLanguages;

    public LanguageModel DefaultLanguage { get; }

    public LanguageModel CurrentLanguage => CreateLanguageModel(Thread.CurrentThread.CurrentUICulture);

    public IEnumerable<LanguageModel> AllLanguages => _availableLanguages.Value.Values;

    public LanguageManager(LanguagesConfiguration configuration)
    {
        _configuration = configuration;
        _availableLanguages = new Lazy<Dictionary<string, LanguageModel>>(GetAvailableLanguages);

        DefaultLanguage = CreateLanguageModel(CultureInfo.GetCultureInfo("en"));
    }

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            throw new ArgumentException($"{nameof(languageCode)} can't be empty.");
        }

        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(languageCode);
    }

    public void SetLanguage(LanguageModel languageModel) => SetLanguage(languageModel.Code);

    private Dictionary<string, LanguageModel> GetAvailableLanguages()
    {
        return _configuration
            .AvailableLocales
            .Select(locale => CreateLanguageModel(new CultureInfo(locale)))
            .ToDictionary(lm => lm.Code, lm => lm);
    }
        

    private LanguageModel CreateLanguageModel(CultureInfo cultureInfo)
    {
        return new LanguageModel(
            cultureInfo.EnglishName, 
            cultureInfo.NativeName.ToTitleCase(),
            cultureInfo.TwoLetterISOLanguageName);
    }
}