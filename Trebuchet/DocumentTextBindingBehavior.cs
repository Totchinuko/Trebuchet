using System;
using Avalonia;
using Avalonia.Media.TextFormatting;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using ITextSource = Trebuchet.ViewModels.ITextSource;

namespace Trebuchet
{
    public class DocumentTextBindingBehavior : Behavior<TextEditor>
    {
        private TextEditor? _textEditor;

        public static readonly StyledProperty<ITextSource?> TextSourceProperty =
            AvaloniaProperty.Register<DocumentTextBindingBehavior, ITextSource?>(nameof(TextSource));

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
            TextSourceProperty.Changed.AddClassHandler<DocumentTextBindingBehavior, ITextSource?>(OnTextSourceChanged);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject is not { } textEditor) return;
            if(TextSource is not null)
                TextSource.LineAppended -= OnLineAppended;
        }

        private void OnTextSourceChanged(DocumentTextBindingBehavior sender, AvaloniaPropertyChangedEventArgs<ITextSource?> args)
        {
            if (_textEditor is not { Document: not null }) return;
            
            if (args.OldValue.Value is { } previous)
                previous.LineAppended -= OnLineAppended;
            
            if (args.NewValue.Value is not { } current) return;
            current.LineAppended += OnLineAppended;
            _textEditor.Clear();
            _textEditor.AppendText(current.Text);

        }

        private void OnLineAppended(object? sender, string line)
        {
            if (_textEditor is not { Document: not null } || TextSource is null) return;
            
            var caretOffset = _textEditor.CaretOffset;
            if(_textEditor.Document.LineCount == 200)
                _textEditor.Document.Remove(
                    _textEditor.Document.GetLineByNumber(1));
            _textEditor.AppendText(line);
            _textEditor.CaretOffset = caretOffset;
            if(TextSource.AutoScroll)
                _textEditor.ScrollToEnd();
        }
    }
}