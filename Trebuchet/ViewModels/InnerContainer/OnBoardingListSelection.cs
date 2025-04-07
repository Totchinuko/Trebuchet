using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingListSelection : DialogueContent
{
    private string _selectedElement = string.Empty;

    public OnBoardingListSelection(string title, string description, List<string> list) : base()
    {
        Title = title;
        Description = description;
        List = new ObservableCollection<string>(list);
        SelectedElement = list.FirstOrDefault(string.Empty);
        ConfirmCommand = ReactiveCommand.Create(Close);
    }
    
    public ObservableCollection<string> List { get; }
    public string Title { get; }
    public string Description { get; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    public string SelectedElement
    {
        get => _selectedElement;
        set => this.RaiseAndSetIfChanged(ref _selectedElement, value);
    }
}