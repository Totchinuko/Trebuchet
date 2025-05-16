using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using tot_lib.CommandLine;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.SettingFields;

public class SequenceEditorField : DescriptiveElement<SequenceEditorField>, IRefreshableField
{
    public SequenceEditorField(string labelTemplate, Sequence sequence, ReactiveCommand<Sequence,Unit> command)
    {
        Sequence = sequence;
        Command = command;
        _count = sequence.Actions.Count;
        _label = string.Format(labelTemplate, _count);
        Update = ReactiveCommand.Create(() =>
        {
            Count = sequence.Actions.Count;
            Label = string.Format(labelTemplate, Count);
        });
    }
    
    private string _label;
    private int _count;

    public ReactiveCommand<Unit, Unit> Update { get; }

    public Sequence Sequence { get; }
    public ReactiveCommand<Sequence,Unit> Command { get; }
    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    public string Label
    {
        get => _label;
        set => this.RaiseAndSetIfChanged(ref _label, value);
    }
}