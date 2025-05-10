using System.Threading.Tasks;
using tot_gui_lib;

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
    
    public void Show(DialogueContent popup)
    {
        Popup?.OnClose();
        Popup = popup;
        Popup.CloseRequested += (_, _) => Close();
        Popup.OnOpen();
    }

    public async Task OpenAsync(DialogueContent popup)
    {
        Show(popup);
        await tot_lib.Utils.WaitUntil(() => !Active);
        Close();
    }

    public void Close()
    {
        Popup?.OnClose();
        Popup = null;
    }
}