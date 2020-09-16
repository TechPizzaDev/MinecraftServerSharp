using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MCServerSharp.Collections
{
    public class ReadOnlyList<T> : ReadOnlyCollection<T>
    {
        private static ReadOnlyList<T>? _empty;

        public static ReadOnlyList<T> Empty
        {
            get
            {
                if (_empty == null)
                    // we don't care about threading; concurrency can only cause some extra allocs here
                    _empty = new ReadOnlyList<T>(new List<T>());

                return _empty;
            }
        }

        protected new List<T> Items => (List<T>)base.Items;

        public ReadOnlyList(List<T> list) : base(list)
        {
        }

        public new List<T>.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
