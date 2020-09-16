using MCServerSharp.Data.IO;

namespace MCServerSharp.World
{
    public interface IBlockPalette
    {
        int BitsPerBlock { get; }

        uint IdForState(BlockState state);
        BlockState StateForId(uint id);

        void Read(NetBinaryReader reader);
        void Write(NetBinaryWriter writer);
        int GetEncodedSize();
    }
}
