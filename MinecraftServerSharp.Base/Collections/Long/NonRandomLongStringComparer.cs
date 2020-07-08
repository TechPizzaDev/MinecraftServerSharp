using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.Collections
{
    public sealed class NonRandomLongStringComparer : LongEqualityComparer<string?>
    {
        public static new ILongEqualityComparer<string?> Default { get; } =
            new NonRandomLongStringComparer();

        private NonRandomLongStringComparer()
        {
        }

        // Copied from .NET Foundation (and Modified)
        // (string.GetNonRandomizedHashCode)
        private static (uint h1, uint h2) Hash(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
                return (0, 0);

            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            var ints = MemoryMarshal.Cast<char, uint>(span);
            int intCount = ints.Length;
            while (ints.Length >= 2)
            {
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ints[0];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ints[1];
                ints = ints.Slice(2);
            }
            span = span.Slice((intCount - ints.Length) * 2);

            for (int i = 0; i < span.Length; i++)
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ span[i];

            return (hash1, hash2);
        }

        public override unsafe int GetHashCode(string? value)
        {
            if (value == null)
                return 0;

            var (h1, h2) = Hash(value.AsSpan());
            return (int)(h1 + (h2 * 1566083941));
        }

        public override unsafe long GetLongHashCode(string? value)
        {
            if (value == null)
                return 0;

            var (h1, h2) = Hash(value.AsSpan());
            return (long)((ulong)h2 << 32 | h1);
        }
    }
}
