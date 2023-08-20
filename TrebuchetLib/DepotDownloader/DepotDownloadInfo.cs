/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 SteamRE https://opensteamworks.org
/// Full license text: LICENSE.txt at the project root
/// Modificatied by Totchinuko under the same license

namespace Trebuchet
{
    internal sealed class DepotDownloadInfo
    {
        public DepotDownloadInfo(uint depotid, uint appId, ulong manifestId, ulong publishedFileId, string branch, string installDir, byte[] depotKey)
        {
            this.id = depotid;
            this.appId = appId;
            this.manifestId = manifestId;
            this.branch = branch;
            this.installDir = installDir;
            this.depotKey = depotKey;
            this.publishedFileId = publishedFileId;
        }

        public uint appId { get; private set; }

        public string branch { get; private set; }

        public byte[] depotKey { get; private set; }

        public uint id { get; private set; }

        public string installDir { get; private set; }

        public ulong manifestId { get; private set; }

        public ulong publishedFileId { get; private set; }
    }
}