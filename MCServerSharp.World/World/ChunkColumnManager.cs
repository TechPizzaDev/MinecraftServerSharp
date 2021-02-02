using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Components;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class ChunkColumnManager : ComponentEntity
    {
        public IChunkColumnProvider ChunkColumnProvider { get; }
        public IChunkProvider ChunkProvider { get; }
        
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

            ChunkProvider = ChunkColumnProvider.CreateChunkProvider();

            //for (int y = 0; y < 16; y++)
            //{
            //    _chunks[y] = new Chunk(this, y, airBlock, blockPalette);
            //}
        }

        public ValueTask<IChunkColumn> GetOrAddChunkColumn(ChunkColumnPosition columnPosition)
        {
            return ChunkColumnProvider.GetOrAddChunkColumn(this, columnPosition);
        }

        public async ValueTask<IChunk> GetOrAddChunk(ChunkPosition position)
        {
            IChunkColumn chunkColumn = await GetOrAddChunkColumn(position.ColumnPosition).Unchain();
            IChunk chunk = await chunkColumn.GetOrAddChunk(position.Y).Unchain();
            return chunk;
        }

        public bool TryGetChunkColumn(ChunkColumnPosition columnPosition, [MaybeNullWhen(false)] out IChunkColumn chunkColumn)
        {
            return ChunkColumnProvider.TryGetChunkColumn(columnPosition, out chunkColumn);
        }

        public bool TryGetChunk(ChunkPosition position, [MaybeNullWhen(false)] out IChunk chunk)
        {
            if (TryGetChunkColumn(position.ColumnPosition, out IChunkColumn? column))
            {
                return column.TryGetChunk(position.Y, out chunk);
            }
            chunk = default;
            return false;
        }
    }
}
