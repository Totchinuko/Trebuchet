using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    public class GeneralMessages
    {
    }

    public class UACPromptRequest : RequestMessage<bool>
    {
        public UACPromptRequest(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; private set; } = string.Empty;
    }
}