using System;
using MCServerSharp.Utility;

namespace MCServerSharp.Blocks
{
    public class BlockState : ILongHashable
    {
        private readonly StatePropertyValue[]? _properties;

        public BlockDescription Description { get; }
        public uint StateId { get; }

        public BlockState DefaultState => Description.DefaultState;
        public uint BlockId => Description.BlockId;
        public Identifier BlockIdentifier => Description.Identifier;

        public ReadOnlyMemory<StatePropertyValue> Properties => _properties;

        public BlockState(BlockDescription block, StatePropertyValue[]? properties, uint id)
        {
            if (properties?.Length == 0)
                properties = null;

            Description = block;
            _properties = properties;
            StateId = id;
        }

        public override string ToString()
        {
            if (_properties != null)
            {
                var builder = _properties.ToListString();
                builder.Insert(0, '[').Insert(0, Description.Identifier.ToString());
                builder.Append(']');
                return builder.ToString();
            }
            return Description.Identifier.ToString();
        }

        public long GetLongHashCode()
        {
            return LongHashCode.Combine(StateId, BlockId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StateId, BlockId);
        }
    }
}
