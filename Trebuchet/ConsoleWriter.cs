using System;
using System.IO;
using System.Text;

namespace Trebuchet;

public class ConsoleWriter : TextWriter
{
    public ConsoleWriter(int buffer, int totalText)
    {
        _totalText = totalText;
        _builder = new(buffer, buffer);
    }
    
    private readonly StringBuilder _builder;
    private readonly int _totalText;

    public event EventHandler<string>? TextFlushed;

    public string Text { get; private set; } = string.Empty;
    
    public override Encoding Encoding { get; } = Encoding.UTF8;

    public override void Flush()
    {
        var flush = _builder.ToString();
        _builder.Clear();
        
        TextFlushed?.Invoke(this, flush);
        Text += flush;
        if (Text.Length > _totalText)
            Text = Text[^_totalText..];
    }

    public override void Write(char value)
    {
        _builder.Append(value);
        if (_builder.Length == _builder.MaxCapacity)
            Flush();
    }

    public void Clear()
    {
        _builder.Clear();
        Text = string.Empty;
    }
}