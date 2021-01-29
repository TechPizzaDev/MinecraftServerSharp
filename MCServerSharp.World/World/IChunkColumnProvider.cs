using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunkColumnProvider
    {
        event Action<IChunkColumnProvider, IChunkColumn> ChunkAdded;
        event Action<IChunkColumnProvider, IChunkColumn> ChunkRemoved;

        ValueTask<ChunkStatus> GetChunkColumnStatus(ChunkColumnPosition columnPosition);

        ValueTask<IChunkColumn> GetOrAddChunkColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition);

        bool TryGetChunkColumn(ChunkColumnPosition columnPosition, [MaybeNullWhen(false)] out IChunkColumn chunkColumn);

        ValueTask<IChunkColumn?> RemoveChunkColumn(ChunkColumnPosition columnPosition);
    }
}
