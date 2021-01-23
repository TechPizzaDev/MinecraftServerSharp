using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Collections;
using MCServerSharp.Maths;
using MCServerSharp.World;

namespace MCServerSharp.Server
{
    public class MemoryChunkColumn : IChunkColumn
    {
        private ConcurrentDictionary<int, Chunk> _chunks;

        public Chunk? this[int chunkY] => TryGetChunk(chunkY);

        public ChunkColumnManager ColumnManager { get; }
        public ChunkColumnPosition Position { get; }

        public ReadOnlyConcurrentDictionary<int, Chunk> Chunks { get; }

        public DirectBlockPalette GlobalBlockPalette => ColumnManager.GlobalBlockPalette;
        public Dimension Dimension => ColumnManager.Dimension;
        public int X => Position.X;
        public int Z => Position.Z;

        // TODO: fix this funky constructor mess (needs redesign)

        public MemoryChunkColumn(ChunkColumnManager manager, ChunkColumnPosition position)
        {
            ColumnManager = manager ?? throw new ArgumentNullException(nameof(manager));
            Position = position;

            _chunks = new ConcurrentDictionary<int, Chunk>();
            Chunks = _chunks.AsReadOnlyDictionary();
        }

        public ValueTask<Chunk> GetOrAddChunk(int chunkY)
        {
            if (!_chunkColumns.TryGetValue(position, out var chunkColumn))
            {
                chunkColumn = new ChunkColumn(this, new ChunkColumnPosition());
                _chunkInfos.Add(chunkColumn, new ChunkInfo());

                int y = 0;
                Chunk chunk0 = await chunkColumn.GetOrAddChunk(y).Unchain();

                chunk0.FillBlockLevel(GlobalBlockPalette["minecraft:bedrock"].DefaultState, y++);

                for (int j = 0; j < 3; j++)
                    chunk0.FillBlockLevel(GlobalBlockPalette["minecraft:dirt"].DefaultState, y++);

                chunk0.FillBlockLevel(GlobalBlockPalette["minecraft:grass_block"].DefaultState, y++);

                _chunkColumns.Add(position, chunkColumn);
            }

            var chunkInfo = _chunkInfos[chunkColumn];
            chunkInfo.Age = 0;
            return chunkColumn;
        }

        public Chunk? TryGetChunk(int chunkY)
        {
            if (_chunks.TryGetValue(chunkY, out Chunk? section))
                return section;
            return null;
        }

        public int GetBiome(int x, int y, int z)
        {
            return 127; // VOID
        }
    }
}
