using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public class ReadOnlyDictionary<TKey, TValue> : System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private static ReadOnlyDictionary<TKey, TValue>? _empty;

        public static ReadOnlyDictionary<TKey, TValue> Empty
        {
            get
            {
                if (_empty == null)
                    // we don't care about threading; concurrency can only cause some extra allocs here
                    _empty = new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

                return _empty;
            }
        }

        protected new Dictionary<TKey, TValue> Dictionary => (Dictionary<TKey, TValue>)base.Dictionary;

        public new Dictionary<TKey, TValue>.KeyCollection Keys => Dictionary.Keys;
        
        public new Dictionary<TKey, TValue>.ValueCollection Values => Dictionary.Values;

        public ReadOnlyDictionary(Dictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }
        
        public new Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }
    }
}
