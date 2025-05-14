using System.Text.Json.Serialization;

namespace TrebuchetLib.Sequences;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ActionType")]
public interface ISequenceAction
{
    Task Execute(SequenceArgs args);
}