using System;
using MCServerSharp.Utility;

namespace MCServerSharp.Blocks
{
    public class BlockState : ILongHashable
    {
        private readonly StatePropertyValue[]? _properties;

        public BlockDescription Block { get; }
        public uint StateId { get; }

        public BlockState DefaultState => Block.DefaultState;
        public uint BlockId => Block.BlockId;
        public Identifier BlockIdentifier => Block.Identifier;

        public ReadOnlyMemory<StatePropertyValue> Properties => _properties;

        public BlockState(BlockDescription block, StatePropertyValue[]? properties, uint id)
        {
            if (properties?.Length == 0)
                properties = null;

            Block = block;
            _properties = properties;
            StateId = id;
        }

        public override string ToString()
        {
            if (_properties != null)
            {
                var builder = _properties.ToListString();
                builder.Insert(0, '[').Insert(0, Block.Identifier.ToString());
                builder.Append(']');
                return builder.ToString();
            }
            return Block.Identifier.ToString();
        }

        public long GetLongHashCode()
        {
            return LongHashCode.Combine(StateId, BlockId, BlockIdentifier);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StateId, BlockId, BlockIdentifier);
        }
    }
}
