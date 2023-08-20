using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
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
