using System;
using Avalonia.Layout;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class DialogueContent : ReactiveObject
{
    public HorizontalAlignment HorizontalAlignment { get; protected set; } = HorizontalAlignment.Center;
    public VerticalAlignment VerticalAlignment { get; protected set; } =  VerticalAlignment.Center;
    public int Width { get; protected set; }
    public int Height { get; protected set; }

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

    public T SetSize<T>(int width, int height) where T : DialogueContent
    {
        Width = width;
        Height = height;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        return (T)this;
    }

    public T SetStretch<T>() where T : DialogueContent
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Width = -1;
        Height = -1;
        return (T)this;
    }
}