/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 SteamRE https://opensteamworks.org
/// Full license text: LICENSE.txt at the project root
/// Modificatied by Totchinuko under the same license

namespace Trebuchet
{
    internal class DepotFilesData
    {
        public HashSet<string> allFileNames = new HashSet<string>();
        public DepotDownloadCounter depotCounter;
        public DepotDownloadInfo depotDownloadInfo;
        public List<ProtoManifest.FileData> filteredFiles = new List<ProtoManifest.FileData>();
        public ProtoManifest? manifest;
        public ProtoManifest? previousManifest;
        public string stagingDir = string.Empty;

        public DepotFilesData(DepotDownloadInfo depotDownloadInfo, DepotDownloadCounter depotCounter)
        {
            this.depotDownloadInfo = depotDownloadInfo;
            this.depotCounter = depotCounter;
        }
    }
}