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
        TextSourceProperty.Changed.AddClassHandler<ConsoleTextBindingBehavior, ITextSource?>(OnTextSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is not { } textEditor) return;
        if(TextSource is not null)
            TextSource.LineAppended -= OnLineAppended;
    }

    private void OnTextSourceChanged(ConsoleTextBindingBehavior sender, AvaloniaPropertyChangedEventArgs<ITextSource?> args)
    {
        if (_textEditor is not { Document: not null }) return;
            
        if (args.OldValue.Value is { } previous)
            previous.LineAppended -= OnLineAppended;
            
        if (args.NewValue.Value is not { } current) return;
        current.LineAppended += OnLineAppended;
        _textEditor.Clear();
        _textEditor.AppendText(current.Text);
        if(current.AutoScroll)
            _textEditor.ScrollToEnd();
    }

    private void OnLineAppended(object? sender, string line)
    {
        if (_textEditor is not { Document: not null } || TextSource is null) return;
            
        var caretOffset = _textEditor.CaretOffset;
        if(_textEditor.Document.LineCount == TextSource.MaxLines)
            _textEditor.Document.Remove(
                _textEditor.Document.GetLineByNumber(1));
        _textEditor.AppendText(line);
        _textEditor.CaretOffset = caretOffset;
        if(TextSource.AutoScroll)
            _textEditor.ScrollToEnd();
    }
}