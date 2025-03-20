using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Trebuchet.Panels;

namespace Trebuchet.Utils;

public class MenuElementSelector : IDataTemplate
{
    public Control? Build(object? param)
    {
        return (param as MenuElement)?.MenuTemplate.Build(param);
    }

    public bool Match(object? data)
    {
        return data is MenuElement;
    }
}