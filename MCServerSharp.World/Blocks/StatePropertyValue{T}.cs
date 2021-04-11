using System;

namespace MCServerSharp.Blocks
{
    public readonly struct StatePropertyValue<T> : IStateProperty<T>, IEquatable<StatePropertyValue<T>>
    {
        public IStateProperty<T> Property { get; }
        public int Index { get; }

        public string Name => Property.Name;
        public Type ElementType => Property.ElementType;
        public int Count => Property.Count;

        public StatePropertyValue(IStateProperty<T> property, int index)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Index = index;
        }

        public int GetIndex(ReadOnlyMemory<char> value)
        {
            return Property.GetIndex(value);
        }

        public StatePropertyValue<T> GetPropertyValue(int index)
        {
            return Property.GetPropertyValue(index);
        }

        public int GetIndex(T value)
        {
            return Property.GetIndex(value);
        }

        public T GetValue(int index)
        {
            return Property.GetValue(index);
        }

        StatePropertyValue IStateProperty.GetPropertyValue(int index)
        {
            return ((IStateProperty)Property).GetPropertyValue(index);
        }

        public bool Equals(StatePropertyValue<T> other)
        {
            return Index != other.Index
                && Property == other.Property;
        }

        public override bool Equals(object? obj)
        {
            return obj is StatePropertyValue<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Property, Index);
        }

        public override string ToString()
        {
            return Name + "=" + Index;
        }

        public static bool operator ==(StatePropertyValue<T> left, StatePropertyValue<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatePropertyValue<T> left, StatePropertyValue<T> right)
        {
            return !(left == right);
        }
    }
}
