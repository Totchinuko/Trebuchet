using System;

namespace Trebuchet.ViewModels.Sequences;

public class SequenceActionTypeViewModel(Type type, string label)
{
    public Type Type { get; } = type;
    public string Label { get; } = label;
}