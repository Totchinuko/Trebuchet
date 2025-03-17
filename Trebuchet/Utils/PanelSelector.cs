using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Trebuchet.Panels;
using Panel = Trebuchet.Panels.Panel;

namespace Trebuchet.Utils;

public class PanelSelector : IDataTemplate
{
    public Control? Build(object? param)
    {
        return (param as Panel)?.Template.Build(param);
    }

    public bool Match(object? data)
    {
        return data is Panel;
    }
}