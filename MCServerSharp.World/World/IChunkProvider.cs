using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunkProvider
    {
        IChunkColumnProvider ColumnProvider { get; }

        ValueTask<IChunk> GetOrAddChunk(ChunkColumnManager columnManager, ChunkPosition chunkPosition);
        
        bool TryGetChunk(ChunkPosition chunkPosition, [MaybeNullWhen(false)] out IChunk chunk);
    }
}
