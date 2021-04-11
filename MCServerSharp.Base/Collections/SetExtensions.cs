using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public static class SetExtensions
    {
        public static ReadOnlySet<T> AsReadOnly<T>(this HashSet<T> set)
        {
            return new ReadOnlySet<T>(set);
        }

        public static ReadOnlySet<T> AsReadOnly<T>(this IReadOnlySet<T> set)
        {
            return new ReadOnlySet<T>(set);
        }

        public static ReadOnlySet<T> AsReadOnly<T>(this ISet<T> set)
        {
            return new ReadOnlySet<T>(set);
        }
    }
}
