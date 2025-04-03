using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TrebuchetUtils;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    protected bool SetProperty<T>(object target, T value, [CallerMemberName] string? propertyName = null)
    {
        var prop = GetPropertyInfos(target, propertyName);
        var propValue = prop.GetValue(target);
        if (propValue is T pValue && EqualityComparer<T>.Default.Equals(pValue, value)) return false;
        prop.SetValue(target, value);
        OnPropertyChanged(propertyName);
        return true;
    }

    protected T GetProperty<T>(object target, [CallerMemberName] string? propertyName = null)
    {
        var prop = GetPropertyInfos(target, propertyName);
        var value = prop.GetValue(target);
        if (value is null)
            throw new NullReferenceException("null is not supported");
        
        return (T)value;
    }
    
    private PropertyInfo GetPropertyInfos(object target, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) 
            throw new ArgumentException(nameof(propertyName));
        
        var prop = target.GetType()
            .GetProperty(propertyName, BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Public);
        if (prop is null) 
            throw new Exception($"Property {propertyName} doesn't exists");
        return prop;
    }
}