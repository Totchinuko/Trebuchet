using System;

namespace Trebuchet.ViewModels;

public interface ITextSource
{
    string Text { get; }
    bool AutoScroll { get; }
    int MaxLines { get; }
    event EventHandler<string>? LineAppended;
}