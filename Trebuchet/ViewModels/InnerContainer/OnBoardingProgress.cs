using System;
using System.Reactive.Linq;
using Avalonia.Threading;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingProgress : DialogueContent, IProgress<double>
{
    private double _progress;
    private readonly ObservableAsPropertyHelper<bool> _isIndeterminate;

    public OnBoardingProgress(string title, string description, double maxValue = 1.0)
    {
        Title = title;
        Description = description;
        MaxValue = maxValue;

        _isIndeterminate = this.WhenAnyValue(x => x.Progress)
            .Select(x => x == 0.0)
            .ToProperty(this, x => x.IsIndeterminate);
    }

    public string Title { get; }
    public string Description { get; }
    public double MaxValue { get; }

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public bool IsIndeterminate => _isIndeterminate.Value;

    public void Report(double value)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Progress = value;
        });
    }
}