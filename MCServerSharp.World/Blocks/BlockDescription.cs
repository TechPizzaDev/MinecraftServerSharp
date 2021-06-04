using System;
using System.Collections.Generic;
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

        public Utf8Identifier Identifier { get; }
        public Identifier IdentifierUtf16 { get; }
        public uint BlockId { get; }

        public int StateCount => _states.Length;
        public int PropertyCount => _properties.Length;
        public BlockState DefaultState => _states[_defaultStateIndex];

        public ReadOnlyMemory<BlockState> States => _states;
        public ReadOnlyMemory<IStateProperty> Properties => _properties;

        public ReadOnlySpan<BlockState> StateSpan => _states;
        public ReadOnlySpan<IStateProperty> PropertySpan => _properties;

        private BlockDescription(
            BlockState[] states, IStateProperty[]? properties,
            Utf8Identifier identifier, Identifier identifierUtf16, uint id, int defaultStateIndex)
        {
            if (!identifier.IsValid)
                throw new ArgumentException("The identifier is not valid.", nameof(identifier));

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _properties = properties ?? Array.Empty<IStateProperty>();
            _defaultStateIndex = defaultStateIndex;
            Identifier = identifier;
            IdentifierUtf16 = identifierUtf16;
            BlockId = id;
        }

        public BlockDescription(
            BlockState[] states, IStateProperty[]? properties,
            Utf8Identifier identifier, uint id, int defaultStateIndex) : this(
                states, properties, identifier, identifier.ToUtf16Identifier(), id, defaultStateIndex)
        {
        }

        public static BlockDescription CreateUnsafe(
            BlockState[] states, IStateProperty[]? properties,
            Utf8Identifier identifier, Identifier identifierUtf16, uint id, int defaultStateIndex)
        {
            if (identifier != identifierUtf16)
            {
                throw new ArgumentException($"The identifiers do not match. \"{identifier}\" != \"{identifierUtf16}\"");
            }
            return new(states, properties, identifier, identifierUtf16, id, defaultStateIndex);
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
                if (prop.Name == name)
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
            if (propertyValues.Length != PropertyCount)
            {
                return DefaultState;
            }

            foreach (BlockState state in _states)
            {
                ReadOnlySpan<StatePropertyValue> sourceValues = state.PropertySpan;

                for (int i = 0; i < propertyValues.Length; i++)
                {
                    int index = sourceValues.IndexOf(propertyValues[i]);
                    if (index == -1)
                        goto Continue;
                }

                return state;

                Continue:
                continue;
            }

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
            return IdentifierUtf16.ToString();
        }
    }
}
