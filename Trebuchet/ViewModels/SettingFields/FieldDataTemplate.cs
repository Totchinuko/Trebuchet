using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Trebuchet.ViewModels.SettingFields;

public class FieldDataTemplate : IDataTemplate
{
    public Control? Build(object? param)
    {
        return (param as Field)?.Template.Build(param);
    }

    public bool Match(object? data)
    {
        return data is Field;
    }
}