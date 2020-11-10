// Copied from .NET Foundation

using System;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Collections
{
    internal sealed class LongHashSetComparer<T> : ILongEqualityComparer<LongHashSet<T>?>
    {
        public bool Equals(LongHashSet<T>? x, LongHashSet<T>? y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x, y))
                return true;

            // They're not both null, so if either is null, they're not equal.
            if (x == null || y == null)
                return false;

            // If both sets use the same comparer, they're equal if they're the same
            // size and one is a "subset" of the other.
            if (LongHashSet<T>.EqualityComparersAreEqual(x, y))
            {
                return x.Count == y.Count && y.IsSubsetOfHashSetWithSameComparer(x);
            }

            // Otherwise, do an O(N^2) match.
            if (typeof(T).IsValueType)
            {
                foreach (T yi in y)
                {
                    bool found = false;
                    foreach (T xi in x)
                    {
                        if (LongEqualityComparer<T>.Default.Equals(yi, xi))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return false;
                }
            }
            else
            {
                var defaultComparer = LongEqualityComparer<T>.Default;
                foreach (T yi in y)
                {
                    bool found = false;
                    foreach (T xi in x)
                    {
                        if (defaultComparer.Equals(yi, xi))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return false;
                }
            }
            return true;
        }

        public int GetHashCode([DisallowNull] LongHashSet<T> obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            int hashCode = 0; // default to 0 for empty set

            foreach (T t in obj)
            {
                if (t != null)
                    hashCode ^= t.GetHashCode(); // same hashcode as as default comparer
            }

            return hashCode;
        }

        public long GetLongHashCode([DisallowNull] LongHashSet<T> obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            long hashCode = 0; // default to 0 for empty set

            var comparer = LongEqualityComparer<T>.Default;
            foreach (T t in obj)
            {
                if (t != null)
                    hashCode ^= comparer.GetLongHashCode(t); // same hashcode as as default comparer
            }

            return hashCode;
        }

        // Equals method for the comparer itself.
        public override bool Equals(object? obj)
        {
            return obj is LongHashSetComparer<T>;
        }

        public override int GetHashCode()
        {
            return LongEqualityComparer<T>.Default.GetHashCode();
        }
    }
}
