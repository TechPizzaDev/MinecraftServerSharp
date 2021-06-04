using System;

namespace MCServerSharp.Blocks
{
    public static class StatePropertyExtensions
    {
        public static StatePropertyValue GetPropertyValue(this IStateProperty property, int index)
        {
            return new StatePropertyValue(property, index);
        }

        public static StatePropertyValue<T> GetPropertyValue<T>(this IStateProperty<T> property, int index)
        {
            return new StatePropertyValue<T>(property, index);
        }

        public static StatePropertyValue GetPropertyValue(this IStateProperty property, ReadOnlyMemory<char> name)
        {
            return property.GetPropertyValue(property.GetIndex(name));
        }
        
        public static StatePropertyValue<T> GetPropertyValue<T>(this IStateProperty<T> property, ReadOnlyMemory<char> name)
        {
            return property.GetPropertyValue(property.GetIndex(name));
        }

        public static StatePropertyValue GetPropertyValue(this IStateProperty property, Utf8Memory name)
        {
            return property.GetPropertyValue(property.GetIndex(name));
        }

        public static StatePropertyValue<T> GetPropertyValue<T>(this IStateProperty<T> property, Utf8Memory name)
        {
            return property.GetPropertyValue(property.GetIndex(name));
        }

        public static StatePropertyValue GetPropertyValue(this IStateProperty property, string? name)
        {
            return property.GetPropertyValue(name.AsMemory());
        }

        public static StatePropertyValue<T> GetPropertyValue<T>(this IStateProperty<T> property, string? name)
        {
            return property.GetPropertyValue(name.AsMemory());
        }
    }
}
