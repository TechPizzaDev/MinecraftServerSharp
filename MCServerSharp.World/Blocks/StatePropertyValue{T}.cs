using System;

namespace MCServerSharp.Blocks
{
    public readonly struct StatePropertyValue<T> : 
        IStateProperty<T>, IEquatable<StatePropertyValue<T>>, IComparable<StatePropertyValue<T>>
    {
        public IStateProperty<T> Property { get; }
        public int Index { get; }

        public string NameUtf16 => Property.NameUtf16;
        public Utf8String Name => Property.Name;
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

        public int GetIndex(Utf8Memory value)
        {
            return Property.GetIndex(value);
        }

        public int GetIndex(T value)
        {
            return Property.GetIndex(value);
        }

        public T GetValue(int index)
        {
            return Property.GetValue(index);
        }
        
        public int CompareTo(StatePropertyValue<T> other)
        {
            return Name.CompareTo(other.Name);
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
            return NameUtf16 + "=" + Index;
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
