using System;
using MCServerSharp.Utility;

namespace MCServerSharp.Blocks
{
    public class BlockState
    {
        private StatePropertyValue[] _properties;

        public BlockDescription Block { get; }
        public uint Id { get; }

        public ReadOnlyMemory<StatePropertyValue> Properties => _properties;

        public BlockState(BlockDescription block, StatePropertyValue[] properties, uint id)
        {
            Block = block ?? throw new ArgumentNullException(nameof(block));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Id = id;
        }

        public override string ToString()
        {
            if (_properties.Length > 0)
            {
                var builder = _properties.ToListString();
                builder.Insert(0, '[').Insert(0, Block.Identifier.ToString());
                builder.Append(']');
                return builder.ToString();
            }
            return Block.Identifier.ToString();
        }
    }
}
