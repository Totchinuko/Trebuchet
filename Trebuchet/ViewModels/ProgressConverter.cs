using System;
using DepotDownloader;

namespace Trebuchet.ViewModels;

public class ProgressConverter(IProgress<double> sink) : IProgress<Progress>
{
    public void Report(Progress value)
    {
        if (value.IsFile) return;
        sink.Report(value.Current / (double)value.Total);
    }
}