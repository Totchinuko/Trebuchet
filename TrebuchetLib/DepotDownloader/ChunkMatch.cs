/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 SteamRE https://opensteamworks.org
/// Full license text: LICENSE.txt at the project root
/// Modificatied by Totchinuko under the same license

namespace Trebuchet
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