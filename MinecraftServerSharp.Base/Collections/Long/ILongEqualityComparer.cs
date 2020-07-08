using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MinecraftServerSharp.Collections
{
    public interface ILongEqualityComparer<in T> : IEqualityComparer<T>
    {
        long GetLongHashCode([DisallowNull] T value);
    }
}
