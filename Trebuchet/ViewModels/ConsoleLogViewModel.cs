using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Splat;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class ConsoleLogViewModel(string body, string color)
{
    public string Body { get; } = body;
    public ObservableCollection<string> Classes { get; } = [@"console", color];
}