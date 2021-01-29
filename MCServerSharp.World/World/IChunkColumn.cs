using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunkColumn
    {
        ChunkColumnManager ColumnManager { get; }
        ChunkColumnPosition Position { get; }

        ValueTask<IChunk> GetOrAddChunk(int chunkY);

        bool ContainsChunk(int chunkY);
        bool TryGetChunk(int chunkY, [MaybeNullWhen(false)] out IChunk chunk);
    }
}
