using System.Collections;
using System.Collections.Generic;

namespace MinecraftServerSharp.Collections
{
    public static class CollectionHelper
    {
        public static int? TryGetCount<T>(IEnumerable<T>? enumerable, bool includeZero = false)
        {
            if (enumerable == null)
                return null;

            int count;

            if (enumerable is IReadOnlyCollection<T> roGColl)
                count = roGColl.Count;
            else if (enumerable is ICollection<T> gColl)
                count = gColl.Count;
            else if (enumerable is IReadOnlyList<T> roGList)
                count = roGList.Count;
            else if (enumerable is ICollection<T> gList)
                count = gList.Count;
            else if (enumerable is ICollection coll)
                count = coll.Count;
            else if (enumerable is IList list)
                count = list.Count;
            else
                return null;

            if (count == 0 && !includeZero)
                return null;

            return count;
        }
    }
}
