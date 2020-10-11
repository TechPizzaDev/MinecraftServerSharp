using System.Buffers;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public interface IBlockPalette
    {
        int BitsPerBlock { get; }
        int Count { get; }

        uint IdForBlock(BlockState state);
        BlockState BlockForId(uint id);

        void Write(NetBinaryWriter writer);
        int GetEncodedSize();
    }
}
