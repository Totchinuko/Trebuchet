using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Threading;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingProgress : InnerPopup, INotifyPropertyChanged, IProgress<double>
{
    private double _progress = 0;

    public OnBoardingProgress(string title, string description) : base("OnBoardingProgress")
    {
        Title = title;
        Description = description;
        SetSize<OnBoardingProgress>(650, 250);
    }

    public string Title { get; }
    public string Description { get; }

    public double Progress
    {
        get => _progress;
        set
        {
            if(SetField(ref _progress, value))
                OnPropertyChanged(nameof(IsIndeterminate));
        }
    }

    public bool IsIndeterminate
    {
        get => Progress == 0.0;
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

    public void Report(double value)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Progress = value;
        });
    }
}