using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using Trebuchet.Services.Language;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingLanguage : ValidatedInputDialogue<LanguageModel, OnBoardingLanguage>
{
    private string _selectedElement = string.Empty;

    public OnBoardingLanguage(string title, string description, List<LanguageModel> list, LanguageModel defaultLanguage) : base(title, description)
    {
        List = new ObservableCollection<LanguageModel>(list);
        Value = list.FirstOrDefault(defaultLanguage);
    }
    
    public ObservableCollection<LanguageModel> List { get; }
}