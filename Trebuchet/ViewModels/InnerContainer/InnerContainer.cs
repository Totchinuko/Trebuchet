using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class InnerContainer : BaseViewModel
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
        Popup?.OnClose();
        Popup = null;
    }
}