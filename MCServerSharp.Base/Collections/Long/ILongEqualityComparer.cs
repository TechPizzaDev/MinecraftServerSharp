using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Collections
{
    public interface ILongEqualityComparer<in T> : IEqualityComparer<T>
    {
        long GetLongHashCode([DisallowNull] T value);
    }
}
