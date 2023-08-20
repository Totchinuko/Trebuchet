using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
