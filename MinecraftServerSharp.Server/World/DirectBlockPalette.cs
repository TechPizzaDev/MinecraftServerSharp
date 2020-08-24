using System.Collections.Generic;
using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.World
{
    public class DirectBlockPalette : IBlockPalette
    {
        public Dictionary<BlockState, uint> _stateToId = new Dictionary<BlockState, uint>();
        public Dictionary<uint, BlockState> _idToState = new Dictionary<uint, BlockState>();

        public int BitsPerBlock { get; } = 14;

        private uint GetGlobalPaletteIdFromState(BlockState state)
        {
            _stateToId.TryGetValue(state, out uint id);
            return id;

            // Implementation left to the user; see Data Generators for more info on the values
        }

        private BlockState GetStateFromGlobalPaletteId(uint id)
        {
            _idToState.TryGetValue(id, out var state);
            return state;

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
