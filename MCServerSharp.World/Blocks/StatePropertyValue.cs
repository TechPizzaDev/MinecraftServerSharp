using System;

namespace MCServerSharp.Blocks
{
    public readonly struct StatePropertyValue : IStateProperty, IEquatable<StatePropertyValue>
    {
        public IStateProperty Property { get; }
        public int Index { get; }

        public string Name => Property.Name;
        public Type ElementType => Property.ElementType;
        public int Count => Property.Count;

        public StatePropertyValue(IStateProperty property, int index)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Index = index;
        }

        public int GetIndex(ReadOnlyMemory<char> value)
        {
            return Property.GetIndex(value);
        }

        public StatePropertyValue GetPropertyValue(int index)
        {
            return Property.GetPropertyValue(index);
        }

        public bool Equals(StatePropertyValue other)
        {
            return Index == other.Index
                && Property == other.Property;
        }

        public override bool Equals(object? obj)
        {
            return obj is StatePropertyValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Property, Index);
        }

        public override string ToString()
        {
            return Name + "=" + Index;
        }

        public static bool operator ==(StatePropertyValue left, StatePropertyValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatePropertyValue left, StatePropertyValue right)
        {
            return !(left == right);
        }

        //public static StatePropertyValue Create(IStateProperty property, int valueIndex)
        //{
        //    if (property == null)
        //        throw new ArgumentNullException(nameof(property));
        //
        //    if (property is IStateProperty<bool> boolProp)
        //        return new StatePropertyValue<bool>(boolProp, valueIndex);
        //
        //    if (property is IStateProperty<int> intProp)
        //        return new StatePropertyValue<int>(intProp, valueIndex);
        //
        //    if (property.ElementType.IsEnum)
        //    {
        //        Type constructType = typeof(StatePropertyValue<>).MakeGenericType(property.ElementType);
        //
        //        if (Activator.CreateInstance(constructType, property, valueIndex) is not StatePropertyValue propertyValue)
        //            throw new Exception("Failed to create property value.");
        //
        //        return propertyValue;
        //    }
        //
        //    return new StatePropertyValue(property, valueIndex);
        //}
    }
}
