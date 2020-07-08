using System.Collections.Generic;

namespace MinecraftServerSharp.Collections
{
    public static class SetExtensions
    {
        public static ReadOnlySet<T> AsReadOnly<T>(this ISet<T> set)
        {
            return new ReadOnlySet<T>(set);
        }
    }
}
