using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrebuchetLib.Services;

public class Progress : IProgress<double>, INotifyPropertyChanged
{
    private double _value;

    public double Value
    {
        get => _value;
        private set => SetField(ref _value, value);
    }

    public void Report(double value)
    {
        Value = value;
    }

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
}