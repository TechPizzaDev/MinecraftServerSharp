using System.Threading.Tasks;
using MCServerSharp.Maths;
using MCServerSharp.World;

namespace MCServerSharp.Server
{
    public class MemoryChunkColumnProvider : IChunkColumnProvider
    {
        public MemoryChunkColumnProvider()
        {
        }

        public ValueTask<IChunkColumn> ProvideChunkColumn(ChunkColumnPosition columnPosition)
        {

        }
    }
}
