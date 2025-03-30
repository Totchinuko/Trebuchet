using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Trebuchet.ViewModels.InnerContainer;

public class InnerContainer : INotifyPropertyChanged
{
    private InnerPopup? _popup;

    public InnerPopup? Popup
    {
        get => _popup;
        private set
        {
            if(SetField(ref _popup, value))
                OnPropertyChanged(nameof(Active));
        }
    }

    public bool Active => Popup != null;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Open(InnerPopup popup)
    {
        Popup?.OnClose();
        Popup = popup;
        Popup.CloseRequested += (_, __) => Close();
        Popup.OnOpen();
    }

    public async Task OpenAsync(InnerPopup popup)
    {
        Open(popup);
        await TrebuchetUtils.Utils.WaitUntil(() => !Active);
        Close();
    }

    public void Close()
    {
        Popup?.Close();
        Popup = null;
    }
    
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