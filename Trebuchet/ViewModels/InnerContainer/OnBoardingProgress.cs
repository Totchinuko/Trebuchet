using System;
using System.Numerics;
using System.Reactive.Linq;
using Avalonia.Threading;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingProgress<T> : DialogueContent, IProgress<T>, IOnBoardingProgress where T : INumber<T>
{
    public OnBoardingProgress(string title, string description, T initialValue, T maxValue)
    {
        Title = title;
        Description = description;
        MaxValue = maxValue;
        _progress = initialValue;

        _isIndeterminate = this.WhenAnyValue(x => x.Progress)
            .Select(x => x == default)
            .ToProperty(this, x => x.IsIndeterminate);

        _currentProgress = this.WhenAnyValue(x => x.Progress, x => x.MaxValue,
                (p, m) => double.CreateChecked(p) / double.CreateChecked(m))
            .ToProperty(this, x => x.CurrentProgress);
    }
    
    private T _progress;
    private readonly ObservableAsPropertyHelper<bool> _isIndeterminate;
    private readonly ObservableAsPropertyHelper<double> _currentProgress;

    public string Title { get; }
    public string Description { get; }
    public double CurrentProgress => _currentProgress.Value;
    public T MaxValue { get; }

    public T Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public bool IsIndeterminate => _isIndeterminate.Value;

    public void Report(T value)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Progress = value;
        });
    }
}