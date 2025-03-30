using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Trebuchet.ViewModels.SettingFields;

public class FieldPanelTemplate : IDataTemplate
{
    public Control? Build(object? param)
    {
        if(param is not Field field) throw new InvalidOperationException("FieldPanelTemplate must be of type Field");
        if(Application.Current is null) throw new ApplicationException("Application.Current must be initialized");
        return !field.DisplayPanel ? field.Template.Build(param) : (Application.Current.Resources["FieldRow"] as IDataTemplate)?.Build(param);
    }

    public bool Match(object? data)
    {
        return data is Field;
    }
}