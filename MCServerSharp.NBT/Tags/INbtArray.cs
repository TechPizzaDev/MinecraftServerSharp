using System;
using System.Collections.Generic;

namespace MCServerSharp.NBT
{
    public interface INbtArray<T> : IReadOnlyList<T>
    {
        new ref readonly T this[int index] { get; }

        ReadOnlyMemory<T> AsMemory();
        ReadOnlySpan<T> AsSpan();
    }
}
