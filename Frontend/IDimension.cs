using System;
using System.Collections.Generic;

namespace Frontend
{
    public interface IDimension
    {
        int Id { get; }

        public Chunk? this[ChunkPosition position] => GetChunk(position);
        Chunk? GetChunk(ChunkPosition position);

        Chunk Load(ChunkPosition position);
        void Unload(ChunkPosition position);

        IEnumerable<Chunk> GetAllChunks();
    }
}