using System.Threading.Tasks;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public interface IChunk
    {
        IChunkColumn Column { get; }
        ChunkPosition Position { get; }
        
        IBlockPalette BlockPalette { get; }

        ChunkCommandList CreateCommandList();
        ValueTask SubmitCommands(ChunkCommandList commandList);
    }
}
