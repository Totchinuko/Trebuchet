using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class ClientConnectionRefViewModel(ClientConnectionRef reference, string source)
{
    public ClientConnectionRef Reference { get; } = reference;
    public string Source { get; } = source;

    public override string ToString()
    {
        return $@"{Source}: {Reference.Connection}";
    }
}