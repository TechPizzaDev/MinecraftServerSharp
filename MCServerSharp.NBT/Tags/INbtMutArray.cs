using System;

namespace MCServerSharp.NBT
{
    public interface INbtMutArray<T> : INbtArray<T>
    {
        new ref T this[int index] { get; }

        new Memory<T> AsMemory();
        new Span<T> AsSpan();
    }
}
