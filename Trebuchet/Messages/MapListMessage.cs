using System.Collections.Generic;
using TrebuchetUtils;

namespace Trebuchet.Messages;

public class MapListMessage : ITinyInstantReturn<Dictionary<string, string>>
{
    public object? Sender { get; } = null;
    public Dictionary<string, string> Value { get; private set; } = [];
    
    public void Respond(Dictionary<string, string> value)
    {
        Value = value;
    }
}