using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MCServerSharp.Collections
{
    /// <summary>
    /// Fast comparer but can create many hash collisions if
    /// values are specially crafted by attackers.
    /// </summary>
    /// <remarks>
    /// Use this if and only if 'Denial of Service' attacks are not a concern 
    /// (i.e. never used for free-form user input),
    /// or are otherwise mitigated.
    /// </remarks>
    public sealed class NonRandomLongUtf8StringComparer : LongEqualityComparer<Utf8String?>
    {
        /// <summary>
        /// Fast hash method but can cause many collisions if specially crafted by attackers.
        /// </summary>
        /// <remarks>
        /// Based on <c>string.GetNonRandomizedHashCode()</c> from .NET.
        /// </remarks>
        public static (uint h1, uint h2) Hash(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
                return (0, 0);

            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            var ints = MemoryMarshal.Cast<byte, uint>(span);
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

        public override int GetHashCode(Utf8String? value)
        {
            if (value == null)
                return 0;

            var (h1, h2) = Hash(value.Bytes);
            return (int)(h1 + (h2 * 1566083941));
        }

        public override long GetLongHashCode(Utf8String? value)
        {
            if (value == null)
                return 0;

            var (h1, h2) = Hash(value.Bytes);
            return (long)((ulong)h2 << 32 | h1);
        }
    }
}
