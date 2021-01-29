using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public class LocalChunkProvider : IChunkProvider
    {
        public LocalChunkColumnProvider ColumnProvider { get; }

        IChunkColumnProvider IChunkProvider.ColumnProvider => ColumnProvider;

        public LocalChunkProvider(LocalChunkColumnProvider columnProvider)
        {
            ColumnProvider = columnProvider ?? throw new ArgumentNullException(nameof(columnProvider));
        }

        public ValueTask<ChunkStatus> GetChunkStatus(ChunkPosition chunkPosition)
        {
            return new ValueTask<ChunkStatus>(ChunkStatus.Unknown);
        }

        public async ValueTask<IChunk> GetOrAddChunk(ChunkColumnManager columnManager, ChunkPosition chunkPosition)
        {
            IChunkColumn column = await ColumnProvider.GetOrAddChunkColumn(columnManager, chunkPosition.ColumnPosition).Unchain();

            throw new NotImplementedException();
        }

        static LocalChunk CreateChunk(int y, LocalChunkColumn column)
        {
            LocalChunk chunk = new LocalChunk(column, y, column.GlobalBlockPalette, column.ColumnManager.Air);
            return chunk;
        }

        public bool TryGetChunk(ChunkPosition chunkPosition, [MaybeNullWhen(false)] out IChunk chunk)
        {
            if (ColumnProvider.TryGetChunkColumn(chunkPosition.ColumnPosition, out IChunkColumn? column))
            {
                return column.TryGetChunk(chunkPosition.Y, out chunk);
            }
            chunk = default;
            return false;
        }
    }
}
