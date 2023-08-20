namespace Goog
{
    internal sealed class DepotDownloadInfo
    {
        public DepotDownloadInfo(
            uint depotid, uint appId, ulong manifestId, string branch,
            string installDir, byte[] depotKey)
        {
            this.id = depotid;
            this.appId = appId;
            this.manifestId = manifestId;
            this.branch = branch;
            this.installDir = installDir;
            this.depotKey = depotKey;
        }

        public uint appId { get; private set; }

        public string branch { get; private set; }

        public byte[] depotKey { get; private set; }

        public uint id { get; private set; }

        public string installDir { get; private set; }

        public ulong manifestId { get; private set; }
    }
}