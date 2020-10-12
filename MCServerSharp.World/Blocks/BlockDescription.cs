using System;

namespace MCServerSharp.Blocks
{
    public class BlockDescription
    {
        private BlockState[] _states;
        private IStateProperty[] _properties;
        private int _defaultStateIndex;

        public Identifier Identifier { get; }
        public uint Id { get; }

        public int StateCount => _states.Length;
        public BlockState DefaultState => _states[_defaultStateIndex];

        public ReadOnlyMemory<BlockState> States => _states;
        public ReadOnlyMemory<IStateProperty> Properties => _properties;

        public BlockDescription(
            BlockState[] states, IStateProperty[] properties, 
            Identifier identifier, uint id, int defaultStateIndex)
        {
            if (!identifier.IsValid)
                throw new ArgumentException("The identifier is not valid.", nameof(identifier));

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _defaultStateIndex = defaultStateIndex;
            Identifier = identifier;
            Id = id;
        }

        public ReadOnlySpan<BlockState> GetStateSpan()
        {
            return _states.AsSpan();
        }
    }
}
