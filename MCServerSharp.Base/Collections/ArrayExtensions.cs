
namespace MCServerSharp.Collections
{
    public static class ArrayExtensions
    {
        public static ArrayEnumerator<T> GetArrayEnumerator<T>(this T[] array)
        {
            return new ArrayEnumerator<T>(array);
        }
    }
}
