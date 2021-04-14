using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Blocks
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class BlockDescription : ILongHashable
    {
        private readonly BlockState[] _states;
        private readonly IStateProperty[] _properties;
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

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _properties = properties ?? Array.Empty<IStateProperty>();
            _defaultStateIndex = defaultStateIndex;
            Identifier = identifier;
            BlockId = id;
        }

        public IStateProperty GetProperty(Utf8Memory name)
        {
            if (TryGetProperty(name, out IStateProperty? property))
                return property;
            throw new KeyNotFoundException();
        }

        public bool TryGetProperty(Utf8Memory name, [MaybeNullWhen(false)] out IStateProperty property)
        {
            foreach (IStateProperty prop in _properties)
            {
                if (prop.NameUtf8 == name)
                {
                    property = prop;
                    return true;
                }
            }
            property = null;
            return false;
        }

        public BlockState GetMatchingState(ReadOnlySpan<StatePropertyValue> propertyValues)
        {
            foreach (BlockState state in _states)
            {
                if (state.Properties.Span.SequenceEqual(propertyValues))
                {
                    return state;
                }
            }

            Debug.Assert(propertyValues.Length == _properties.Length);
            return DefaultState;
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
