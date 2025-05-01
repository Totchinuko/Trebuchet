using System;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace Trebuchet.ViewModels;

public class ConsoleTextBindingBehavior : Behavior<TextEditor>
{
    private TextEditor? _textEditor;

    public static readonly StyledProperty<ITextSource?> TextSourceProperty =
        AvaloniaProperty.Register<ConsoleTextBindingBehavior, ITextSource?>(nameof(TextSource));

    public ITextSource? TextSource
    {
        get => GetValue(TextSourceProperty);
        set => SetValue(TextSourceProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } textEditor) return;
        _textEditor = textEditor;
        _textEditor.Options.AllowScrollBelowDocument = false;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is not { } textEditor) return;
        if(TextSource is not null)
            TextSource.TextAppended -= OnTextAppended;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_textEditor is not { Document: not null }) return;

        if (change.Property == TextSourceProperty)
        {
            if (change.OldValue is ITextSource previous)
            {
                previous.TextAppended -= OnTextAppended;
                previous.TextCleared -= OnTextCleared;
            }
            
            if (change.NewValue is not ITextSource current) return;
            current.TextAppended += OnTextAppended;
            current.TextCleared += OnTextCleared;
            _textEditor.Clear();
            _textEditor.AppendText(current.Text);
            if(current.AutoScroll)
                _textEditor.ScrollToEnd();
        }
    }

    private void OnTextCleared(object? sender, EventArgs e)
    {
        if (_textEditor is not { Document: not null } || TextSource is null) return;
        _textEditor.Clear();
    }

    private void OnTextAppended(object? sender, string text)
    {
        if (_textEditor is not { Document: not null } || TextSource is null) return;
            
        var caretOffset = _textEditor.CaretOffset;
        _textEditor.BeginChange();
        while(_textEditor.Document.TextLength > TextSource.MaxChar)
            _textEditor.Document.Remove(_textEditor.Document.GetLineByNumber(0));
        _textEditor.AppendText(text);
        _textEditor.EndChange();
        _textEditor.CaretOffset = caretOffset;
        if(TextSource.AutoScroll)
            _textEditor.ScrollToEnd();
    }
}