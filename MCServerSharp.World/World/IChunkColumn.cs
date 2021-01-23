using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunkColumn
    {
        ChunkColumnManager ColumnManager { get; }
        ChunkColumnPosition Position { get; }

        Chunk? this[int chunkY] => TryGetChunk(chunkY);

        ValueTask<Chunk> GetOrAddChunk(int chunkY);
        Chunk? TryGetChunk(int chunkY);
    }
}
