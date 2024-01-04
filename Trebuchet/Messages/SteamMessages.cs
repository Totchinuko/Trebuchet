using CommunityToolkit.Mvvm.Messaging.Messages;
using SteamWorksWebAPI;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Documents;

namespace Trebuchet
{
    public class SteamConnectionChangedMessage : ValueChangedMessage<bool>
    {
        public SteamConnectionChangedMessage(bool value) : base(value)
        {
        }
    }

    public class SteamConnectMessage
    {
    }

    public class SteamModlistIDRequest
    {
        public IEnumerable<ulong> Modlist;

        public SteamModlistIDRequest(IEnumerable<ulong> modlist)
        {
            Modlist = modlist;
        }
    }

    public class SteamModlistReceived
    {
        public List<PublishedFile> Modlist;

        public SteamModlistReceived(PublishedFilesResponse response)
        {
            Modlist = new List<PublishedFile>(response.PublishedFileDetails);
        }
    }

    public class SteamModlistRequest
    {
        public string modlist = string.Empty;

        public SteamModlistRequest(string modlist)
        {
            this.modlist = modlist;
        }
    }

    public class SteamModlistUpdateRequest : RequestMessage<List<ulong>>
    {
        public IEnumerable<(ulong PubID, ulong manifestID)> keyValuePairs;

        public SteamModlistUpdateRequest(IEnumerable<(ulong PubID, ulong manifestID)> keyValuePairs)
        {
            this.keyValuePairs = keyValuePairs;
        }
    }
}