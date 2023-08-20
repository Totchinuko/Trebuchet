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