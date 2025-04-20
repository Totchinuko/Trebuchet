using System;

namespace Trebuchet.ViewModels;

public interface ITextSource
{
    string Text { get; }
    bool AutoScroll { get; }
    int MaxChar { get; }
    event EventHandler<string>? TextAppended;
    event EventHandler? TextCleared;
}