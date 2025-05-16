using System;

namespace Trebuchet.ViewModels.Sequences;

[AttributeUsage(validOn:AttributeTargets.Class)]
public class SequenceActionAttribute(Type target) : Attribute
{
    public Type Target { get; } = target;
}