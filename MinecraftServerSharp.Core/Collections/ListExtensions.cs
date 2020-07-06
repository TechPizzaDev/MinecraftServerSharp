using System.Collections.Generic;

namespace MinecraftServerSharp.Collections
{
    public static class ListExtensions
    {
        public static ReadOnlyList<T> AsReadOnlyList<T>(this List<T> list)
        {
            return new ReadOnlyList<T>(list);
        }
    }
}
