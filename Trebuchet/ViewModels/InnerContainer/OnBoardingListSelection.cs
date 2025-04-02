using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingListSelection : InnerPopup
{
    private string _selectedElement;

    public OnBoardingListSelection(string title, string description, List<string> list) : base()
    {
        Title = title;
        Description = description;
        List = new ObservableCollection<string>(list);
        _selectedElement = list.FirstOrDefault(string.Empty);
        ConfirmCommand = new SimpleCommand().Subscribe(Close);
    }
    
    public ObservableCollection<string> List { get; }
    public string Title { get; }
    public string Description { get; }
    public SimpleCommand ConfirmCommand { get; }
    public string SelectedElement
    {
        get => _selectedElement;
        set => SetField(ref _selectedElement, value);
    }
}