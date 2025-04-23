using System.Reactive.Linq;
using Humanizer;
using ReactiveUI;

namespace Trebuchet.ViewModels;

public class ModProgressViewModel : ReactiveObject
{
    public ModProgressViewModel()
    {
        _isProgressing = this.WhenAnyValue(x => x.Progress)
            .Select(x => x >= 0)
            .ToProperty(this, x => x.IsProgressing);
    }
    
    private readonly ObservableAsPropertyHelper<bool> _isProgressing;
    private double _progress = -1;
    private string _progressLabel = string.Empty;
    private bool _isIndeterminate;

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }
    
    public string ProgressLabel
    {
        get => _progressLabel;
        set => this.RaiseAndSetIfChanged(ref _progressLabel, value);
    }
    
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => this.RaiseAndSetIfChanged(ref _isIndeterminate, value);
    }

    public bool IsProgressing
    {
        get => _isProgressing.Value;
    }

    public void Report(DepotDownloader.Progress progress)
    {
        Progress = progress.Current / (double)progress.Total;
        ProgressLabel = $@"{((long)progress.Current).Bytes().Humanize()}/{((long)progress.Total).Bytes().Humanize()}";
        IsIndeterminate = progress.Total == 0;
    }

    public void ReportEnd()
    {
        ProgressLabel = string.Empty;
        IsIndeterminate = false;
        Progress = -1;
    }
}