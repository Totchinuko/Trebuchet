using System;
using Avalonia.Layout;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class DialogueContent : ReactiveObject
{
    public HorizontalAlignment HorizontalAlignment { get; protected set; } = HorizontalAlignment.Center;
    public VerticalAlignment VerticalAlignment { get; protected set; } =  VerticalAlignment.Center;

    public event EventHandler? CloseRequested;

    public void OnOpen()
    {
    }

    public void OnClose()
    {
    }
    
    public void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public T SetStretch<T>() where T : DialogueContent
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        return (T)this;
    }
}