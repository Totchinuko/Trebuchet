using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingListSelection : ValidatedInputDialogue<string, OnBoardingListSelection>
{
    private string _selectedElement = string.Empty;

    public OnBoardingListSelection(string title, string description, List<string> list) : base(title, description)
    {
        List = new ObservableCollection<string>(list);
        Value = list.FirstOrDefault(string.Empty);
    }
    
    public ObservableCollection<string> List { get; }
}