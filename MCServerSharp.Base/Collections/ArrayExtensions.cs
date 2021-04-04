using MCServerSharp.Collections;

namespace MCServerSharp
{
    public static class ArrayExtensions
    {
        public static ArrayEnumerable<T> GetEnumerable<T>(this T[] array)
        {
            return new ArrayEnumerable<T>(array);
        }
    }
}
