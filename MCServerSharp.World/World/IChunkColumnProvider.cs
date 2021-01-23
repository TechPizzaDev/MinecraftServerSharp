using System.Threading.Tasks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunkColumnProvider
    {
        ValueTask<IChunkColumn> ProvideChunkColumn(ChunkColumnPosition columnPosition);
    }
}
