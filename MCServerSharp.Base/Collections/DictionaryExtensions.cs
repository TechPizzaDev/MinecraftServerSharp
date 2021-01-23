using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public static class DictionaryExtensions
    {
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            return new ReadOnlyDictionary<TKey, TValue>(dictionary);
        }

        public static ReadOnlyConcurrentDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            return new ReadOnlyConcurrentDictionary<TKey, TValue>(dictionary);
        }
    }
}
