using System;
using MCServerSharp.Memory;

namespace MCServerSharp
{
    public static class MemoryExtensions
    {
        public static MemoryEnumerable<T> GetEnumerable<T>(this ReadOnlyMemory<T> memory)
        {
            return new MemoryEnumerable<T>(memory);
        }

        public static MemoryEnumerable<T> GetEnumerable<T>(this Memory<T> memory)
        {
            return new MemoryEnumerable<T>(memory);
        }
    }
}
