using System.Collections.Generic;

namespace TrebuchetUtils.Services.Language;

public interface ILanguageManager
{
    LanguageModel CurrentLanguage { get; }

    LanguageModel DefaultLanguage { get; }

    IEnumerable<LanguageModel> AllLanguages { get; }

    void SetLanguage(string languageCode);

    void SetLanguage(LanguageModel languageModel);
}