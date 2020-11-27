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
    }
}
