using System.Collections.Generic;
using System.Text;

namespace MCServerSharp.Utility
{
    public static class EnumerableExtensions
    {
        public static string ToListString<T>(this IEnumerable<T> items, string separator = ", ")
        {
            var builder = new StringBuilder();
            builder.AppendJoin(separator, items);
            return builder.ToString();
        }
    }
}
