using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    internal class ChunkMatch
    {
        public ChunkMatch(ProtoManifest.ChunkData oldChunk, ProtoManifest.ChunkData newChunk)
        {
            OldChunk = oldChunk;
            NewChunk = newChunk;
        }

        public ProtoManifest.ChunkData NewChunk { get; private set; }

        public ProtoManifest.ChunkData OldChunk { get; private set; }
    }
}
