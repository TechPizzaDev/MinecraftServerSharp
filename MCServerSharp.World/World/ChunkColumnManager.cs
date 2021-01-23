using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Components;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class ChunkColumnManager : ComponentEntity
    {
        private SemaphoreSlim _columnLock = new(4);

        private ConcurrentDictionary<ChunkColumnPosition, IChunkColumn> _columns = new();

        public IChunkColumnProvider ChunkColumnProvider { get; }

        // TODO: replace with some kind of registry
        public DirectBlockPalette GlobalBlockPalette { get; }
        public BlockState Air { get; }

        public Dimension Dimension => this.GetComponent<DimensionComponent>().Dimension;

        public ChunkColumnManager(
            IChunkColumnProvider chunkColumnProvider,
            DirectBlockPalette globalBlockPalette)
        {
            ChunkColumnProvider = chunkColumnProvider ?? throw new ArgumentNullException(nameof(chunkColumnProvider));
            GlobalBlockPalette = globalBlockPalette ?? throw new ArgumentNullException(nameof(globalBlockPalette));

            Air = GlobalBlockPalette["minecraft:air"].DefaultState;

            //for (int y = 0; y < 16; y++)
            //{
            //    _chunks[y] = new Chunk(this, y, airBlock, blockPalette);
            //}
        }

        public async ValueTask<IChunkColumn> GetOrAddChunkColumn(ChunkColumnPosition columnPosition)
        {
            if (_columns.TryGetValue(columnPosition, out IChunkColumn? column))
                return column;

            await _columnLock.WaitAsync().Unchain();
            try
            {
                if (!_columns.TryGetValue(columnPosition, out column))
                {
                    column = await ChunkColumnProvider.ProvideChunkColumn(columnPosition).Unchain();
                }
                return column;
            }
            finally
            {
                _columnLock.Release();
            }
        }

        public async ValueTask<Chunk> GetOrAddChunk(ChunkPosition position)
        {
            IChunkColumn chunkColumn = await GetOrAddChunkColumn(position.ColumnPosition).Unchain();
            return await chunkColumn.GetOrAddChunk(position.Y).Unchain();
        }

        public IChunkColumn? TryGetChunkColumn(ChunkColumnPosition columnPosition)
        {
            if (_columns.TryGetValue(columnPosition, out IChunkColumn? column))
                return column;
            return null;
        }

        public Chunk? TryGetChunk(ChunkPosition position)
        {
            IChunkColumn? chunkColumn = TryGetChunkColumn(position.ColumnPosition);
            return chunkColumn?.TryGetChunk(position.Y);
        }
    }
}
