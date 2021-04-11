using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunkRegion
    {
        ValueTask<IChunkColumn?> LoadColumn(ChunkColumnManager columnManager, ChunkColumnPosition columnPosition);
    }
}
