using System;

namespace MCServerSharp.Blocks
{
    public class StatePropertyValue : IStateProperty
    {
        private readonly int _hashCode;

        public IStateProperty Property { get; }
        public int ValueIndex { get; }

        public string Name => Property.Name;

        public StatePropertyValue(IStateProperty property, int valueIndex)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            ValueIndex = valueIndex;

            _hashCode = HashCode.Combine(Property, ValueIndex);
        }

        public int ParseIndex(string value)
        {
            return Property.ParseIndex(value);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return Name + "=" + ValueIndex;
        }

        public static StatePropertyValue Create(IStateProperty property, int valueIndex)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (property is IStateProperty<bool> boolProp)
                return new StatePropertyValue<bool>(boolProp, valueIndex);

            if (property is IStateProperty<int> intProp)
                return new StatePropertyValue<int>(intProp, valueIndex);

            Type propertyType = property.GetType();
            if (propertyType.IsConstructedGenericType)
            {
                Type[] genericTypeArgs = propertyType.GenericTypeArguments;
                if (genericTypeArgs.Length == 1)
                {
                    Type genericType = genericTypeArgs[0];
                    if (genericType.IsEnum && genericType.IsValueType)
                    {
                        Type constructType = typeof(StatePropertyValue<>).MakeGenericType(genericType);

                        if (Activator.CreateInstance(constructType, property, valueIndex) is not StatePropertyValue propertyValue)
                            throw new Exception("Failed to create property value.");

                        return propertyValue;
                    }
                }
            }

            return new StatePropertyValue(property, valueIndex);
        }
    }
}
