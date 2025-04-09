using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class DialogueBox : BaseViewModel
{
    private DialogueContent? _popup;

    public DialogueContent? Popup
    {
        get => _popup;
        private set
        {
            if(SetField(ref _popup, value))
                OnPropertyChanged(nameof(Active));
        }
    }

    public bool Active => Popup != null;
    
    public void Open(DialogueContent popup)
    {
        Popup?.OnClose();
        Popup = popup;
        Popup.CloseRequested += (_, __) => Close();
        Popup.OnOpen();
    }

    public async Task OpenAsync(DialogueContent popup)
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