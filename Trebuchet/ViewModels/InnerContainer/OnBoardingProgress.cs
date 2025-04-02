using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Threading;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingProgress : InnerPopup, IProgress<double>
{
    private double _progress = 0;

    public OnBoardingProgress(string title, string description, double maxValue = 1.0) : base()
    {
        Title = title;
        Description = description;
        MaxValue = maxValue;
        SetSize<OnBoardingProgress>(650, 250);
    }

    public string Title { get; }
    public string Description { get; }
    public double MaxValue { get; }

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

  public void Report(double value)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Progress = value;
        });
    }
}