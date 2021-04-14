using System;
using MCServerSharp.Utility;

namespace MCServerSharp.Blocks
{
    public class BlockState : IEquatable<BlockState>, IComparable<BlockState>, ILongHashable
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

        public int CompareTo(BlockState? other)
        {
            if (other == null)
                return 1;
            return StateId.CompareTo(other.StateId);
        }

        public bool Equals(BlockState? other)
        {
            if (other == null)
                return false;
            return StateId == other.StateId;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as BlockState);
        }

        public long GetLongHashCode()
        {
            return LongHashCode.Combine(StateId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StateId);
        }

        public static bool operator ==(BlockState left, BlockState right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(BlockState left, BlockState right)
        {
            return !(left == right);
        }

        public static bool operator <(BlockState left, BlockState right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        public static bool operator <=(BlockState left, BlockState right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(BlockState left, BlockState right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(BlockState left, BlockState right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
