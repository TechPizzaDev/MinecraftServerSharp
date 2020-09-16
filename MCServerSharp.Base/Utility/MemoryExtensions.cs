using System;

namespace MCServerSharp.Utility
{
    public static class MemoryExtensions
    {
        public static bool IsEmpty(this IReadOnlyMemory memory)
        {
            if (memory == null)
                return true;

            return memory.ByteLength() == 0;
        }

        public static int ByteLength(this IReadOnlyMemory memory)
        {
            if (memory == null)
                throw new ArgumentNullException(nameof(memory));

            return memory.Length * memory.ElementSize;
        }
    }
}
