/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 SteamRE https://opensteamworks.org
/// Full license text: LICENSE.txt at the project root
/// Modificatied by Totchinuko under the same license

namespace Trebuchet
{
    internal class FileStreamData
    {
        public int chunksToDownload;
        public SemaphoreSlim fileLock;
        public FileStream? fileStream;

        public FileStreamData(SemaphoreSlim fileLock, int chunksToDownload, FileStream? stream = null)
        {
            fileStream = stream;
            this.fileLock = fileLock;
            this.chunksToDownload = chunksToDownload;
        }
    }
}