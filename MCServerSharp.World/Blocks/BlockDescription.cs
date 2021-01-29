using System;
using System.Diagnostics;

namespace MCServerSharp.Blocks
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class BlockDescription : ILongHashable
    {
        private readonly BlockState[] _states;
        private readonly IStateProperty[]? _properties;
        private readonly int _defaultStateIndex;

        public Identifier Identifier { get; }
        public uint BlockId { get; }

        public int StateCount => _states.Length;
        public BlockState DefaultState => _states[_defaultStateIndex];

        public ReadOnlyMemory<BlockState> States => _states;
        public ReadOnlyMemory<IStateProperty> Properties => _properties;

        public BlockDescription(
            BlockState[] states, IStateProperty[]? properties, 
            Identifier identifier, uint id, int defaultStateIndex)
        {
            if (!identifier.IsValid)
                throw new ArgumentException("The identifier is not valid.", nameof(identifier));

            if (properties?.Length == 0)
                properties = null;

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _properties = properties;
            _defaultStateIndex = defaultStateIndex;
            Identifier = identifier;
            BlockId = id;
        }

        public long GetLongHashCode()
        {
            return LongHashCode.Combine(BlockId, Identifier);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BlockId, Identifier);
        }

        private string GetDebuggerDisplay()
        {
            return Identifier.ToString();
        }
    }
}
