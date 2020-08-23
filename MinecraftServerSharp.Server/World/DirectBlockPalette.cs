using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.World
{
    public class DirectBlockPalette : IBlockPalette
    {
        public static DirectBlockPalette Instance { get; } = new DirectBlockPalette();

        public int BitsPerBlock { get; } = 8;

        private uint GetGlobalPaletteIdFromState(BlockState state)
        {
            return 1;
            // Implementation left to the user; see Data Generators for more info on the values
        }

        private BlockState GetStateFromGlobalPaletteId(uint value)
        {
            // Implementation left to the user; see Data Generators for more info on the values
            return BlockState.Empty;
        }

        public uint IdForState(BlockState state)
        {
            return GetGlobalPaletteIdFromState(state);
        }

        public BlockState StateForId(uint id)
        {
            return GetStateFromGlobalPaletteId(id);
        }

        public void Read(NetBinaryReader reader)
        {
        }

        public void Write(NetBinaryWriter writer)
        {
        }

        public int GetEncodedSize()
        {
            return 0;
        }
    }
}
