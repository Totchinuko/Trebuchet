using System;

namespace Trebuchet.ViewModels;

public interface ITextSource
{
    string Text { get; }
    event EventHandler<string>? LineAppended;
    bool AutoScroll { get; }
}