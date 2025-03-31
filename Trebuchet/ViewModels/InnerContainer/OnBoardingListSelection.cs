using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingListSelection : InnerPopup, INotifyPropertyChanged
{
    private string _selectedElement;

    public OnBoardingListSelection(string title, string description, List<string> list) : base("OnBoardingListSelection")
    {
        Title = title;
        Description = description;
        List = new ObservableCollection<string>(list);
        _selectedElement = list.FirstOrDefault(string.Empty);
        ConfirmCommand = new SimpleCommand((_) => Close());
    }
    
    public ObservableCollection<string> List { get; }
    public string Title { get; }
    public string Description { get; }
    public ICommand ConfirmCommand { get; }
    public string SelectedElement
    {
        get => _selectedElement;
        set => SetField(ref _selectedElement, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}