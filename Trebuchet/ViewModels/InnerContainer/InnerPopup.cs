using System;
using Avalonia.Layout;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class InnerPopup : BaseViewModel
{
    public HorizontalAlignment HorizontalAlignment { get; protected set; } = HorizontalAlignment.Stretch;
    public VerticalAlignment VerticalAlignment { get; protected set; } =  VerticalAlignment.Stretch;
    public int Width { get; protected set; } = -1;
    public int Height { get; protected set; } = -1;

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

    public T SetSize<T>(int width, int height) where T : InnerPopup
    {
        Width = width;
        Height = height;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        return (T)this;
    }

    public T SetStretch<T>() where T : InnerPopup
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Width = -1;
        Height = -1;
        return (T)this;
    }
}