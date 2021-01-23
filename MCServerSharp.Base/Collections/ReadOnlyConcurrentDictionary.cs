using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public class ReadOnlyConcurrentDictionary<TKey, TValue> : System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private static ReadOnlyConcurrentDictionary<TKey, TValue>? _empty;

        public static ReadOnlyConcurrentDictionary<TKey, TValue> Empty
        {
            get
            {
                if (_empty == null)
                    // we don't care about threading; concurrency can only cause some extra allocs here
                    _empty = new ReadOnlyConcurrentDictionary<TKey, TValue>(new ConcurrentDictionary<TKey, TValue>());

                return _empty;
            }
        }

        protected new ConcurrentDictionary<TKey, TValue> Dictionary => (ConcurrentDictionary<TKey, TValue>)base.Dictionary;

        public new ICollection<TKey> Keys => Dictionary.Keys;
        
        public new ICollection<TValue> Values => Dictionary.Values;

        public ReadOnlyConcurrentDictionary(ConcurrentDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }
        
        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }
    }
}
