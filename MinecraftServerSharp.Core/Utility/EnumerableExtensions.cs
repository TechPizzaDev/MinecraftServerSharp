using System.Collections.Generic;
using System.Text;

namespace MinecraftServerSharp.Utility
{
    public static class EnumerableExtensions
    {
        public static string ToListString<T>(this IEnumerable<T> items)
        {
            var builder = new StringBuilder();
            builder.AppendJoin(", ", items);
            return builder.ToString();
        }
    }
}
