using System;
using MCServerSharp.Collections;
using MCServerSharp.Text;

namespace MCServerSharp
{
    public readonly partial struct Utf8Memory : 
        IComparable<Utf8Memory>, IComparable<Utf8String>,
        IEquatable<Utf8Memory>, IEquatable<Utf8String>, 
        ILongHashable
    {
        public static Utf8Memory Empty => default;

        public ReadOnlyMemory<byte> Memory { get; }
        public ReadOnlySpan<byte> Span => Memory.Span;
        public int Length => Memory.Length;
        public bool IsEmpty => Memory.IsEmpty;

        private Utf8Memory(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public static Utf8Memory CreateUnsafe(ReadOnlyMemory<byte> memory)
        {
            return new Utf8Memory(memory);
        }

        public Utf8RuneEnumerator EnumerateRunes()
        {
            return new Utf8RuneEnumerator(Span);
        }

        public Utf8Memory Substring(int start, int count)
        {
            if (count == 0)
                return default;

            if (start == 0 && count == Length)
                return this;

            if (!Utf8String.IsValidUtf8Slice(Span, start, count))
                throw new ArgumentException("The given range would tear UTF8 sequences.");

            ReadOnlyMemory<byte> slice = Memory.Slice(start, count);
            return new Utf8Memory(slice);
        }

        public Utf8Memory Substring(Range range)
        {
            (int offset, int length) = range.GetOffsetAndLength(Length);
            return Substring(offset, length);
        }

        public int CompareTo(Utf8Memory other)
        {
            return Span.SequenceCompareTo(other.Span);
        }

        public int CompareTo(Utf8String? other)
        {
            return Span.SequenceCompareTo(((Utf8Memory)other).Span);
        }

        public bool Equals(Utf8Memory other, StringComparison comparison)
        {
            if (comparison == StringComparison.Ordinal)
                return Span.SequenceEqual(other.Span);

            return Utf8String.Equals(Span, other.Span, comparison);
        }

        public bool Equals(Utf8Memory other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

        public bool Equals(Utf8String? other, StringComparison comparison)
        {
            return Equals((Utf8Memory)other, comparison);
        }

        public bool Equals(Utf8String? other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is Utf8Memory other && Equals(other)
                || Equals(obj as Utf8String);
        }

        public override int GetHashCode()
        {
            return LongEqualityComparer<Utf8Memory>.Default.GetHashCode(this);
        }

        public long GetLongHashCode()
        {
            return LongEqualityComparer<Utf8Memory>.Default.GetLongHashCode(this);
        }

        /// <summary>
        /// Constructs a new <see cref="string"/> from this <see cref="Utf8Memory"/>.
        /// </summary>
        public override string ToString()
        {
            return StringHelper.Utf8.GetString(Span);
        }

        public static bool operator ==(Utf8Memory left, Utf8Memory right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Utf8Memory left, Utf8Memory right)
        {
            return !(left == right);
        }
    }
}
