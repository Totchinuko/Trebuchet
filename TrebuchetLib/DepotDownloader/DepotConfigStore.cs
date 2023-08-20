using ProtoBuf;
using System.IO.Compression;

/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 SteamRE https://opensteamworks.org
/// Full license text: LICENSE.txt at the project root
/// Modificatied by Totchinuko under the same license

namespace Trebuchet
{
    [ProtoContract]
    internal class DepotConfigStore
    {
        private DepotConfigStore()
        { }

        [ProtoMember(1)]
        public Dictionary<uint, ulong> InstalledManifestIDs { get; } = new Dictionary<uint, ulong>();

        [ProtoMember(2)]
        public Dictionary<ulong, ulong> InstalledUGCManifestIDs { get; } = new Dictionary<ulong, ulong>();

        private string FileName { get; set; } = string.Empty;

        public static DepotConfigStore LoadFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                var store = new DepotConfigStore();
                store.FileName = filename;
                return store;
            }

            using (var fs = File.Open(filename, FileMode.Open))
            using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
            {
                var store = Serializer.Deserialize<DepotConfigStore>(ds);
                store.FileName = filename;
                return store;
            }
        }

        public void Save()
        {
            using (var fs = File.Open(FileName, FileMode.Create))
            using (var ds = new DeflateStream(fs, CompressionMode.Compress))
                Serializer.Serialize(ds, this);
        }
    }
}