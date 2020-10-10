using System.Collections.Generic;
using MCServerSharp.Data.IO;

namespace MCServerSharp.World
{
    // TODO:
    public class DirectBlockPalette : IBlockPalette
    {
        public BlockState[] _states;
        
        public int BitsPerBlock { get; } = 14;

        public int Count => _states.Length;

        private uint GetGlobalPaletteIdFromState(BlockState state)
        {
            return state.Id;

            // Implementation left to the user; see Data Generators for more info on the values
        }

        private BlockState GetStateFromGlobalPaletteId(uint id)
        {
            return _states[id] ?? BlockState.Air;

            // Implementation left to the user; see Data Generators for more info on the values
            return BlockState.Air;
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
