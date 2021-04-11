using System;

namespace MCServerSharp.Blocks
{
    public static class StatePropertyExtensions
    {
        public static StatePropertyValue GetPropertyValue(this IStateProperty property, ReadOnlyMemory<char> name)
        {
            return property.GetPropertyValue(property.GetIndex(name));
        }
        
        public static StatePropertyValue<T> GetPropertyValue<T>(this IStateProperty<T> property, ReadOnlyMemory<char> name)
        {
            return property.GetPropertyValue(property.GetIndex(name));
        }
    }
}
