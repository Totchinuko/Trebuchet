using System;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace Trebuchet.ViewModels.InnerContainer;

public class InnerPopup(string template)
{
    private string _template = template;
    
    public IDataTemplate Template {
        get
        {
            if(Application.Current == null) throw new Exception("Application.Current is null");

            if (Application.Current.Resources.TryGetResource(_template, Application.Current.ActualThemeVariant,
                    out var resource) && resource is IDataTemplate t)
            {
                return t;
            }

            throw new Exception($"Template {_template} not found");
        }
    }
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
}