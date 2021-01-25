using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using MCServerSharp.Collections;
using MCServerSharp.Text;

namespace MCServerSharp
{
    // TODO: string interning

    [DebuggerDisplay("{ToString()}")]
    [SkipLocalsInit]
    public partial class Utf8String : IComparable<Utf8String>, IEquatable<Utf8String>, ILongHashable
    {
        public static Utf8String Empty { get; } = new Utf8String(Array.Empty<byte>());

        private byte[]? _byteArray;
        private ReadOnlyMemory<byte> _bytes;

        public ReadOnlySpan<byte> Bytes => _bytes.Span;
        public int Length => _bytes.Length;

        #region Constructors

        private Utf8String(byte[] bytes)
        {
            _byteArray = bytes;
            _bytes = _byteArray.AsMemory();
        }

        private Utf8String(ReadOnlyMemory<byte> bytes)
        {
            _bytes = bytes;
        }

        private Utf8String(int length) : this(length == 0 ? Array.Empty<byte>() : new byte[length])
        {
        }

        public Utf8String(string value) : this(StringHelper.Utf8.GetByteCount(value))
        {
            StringHelper.Utf8.GetBytes(value, _byteArray);
        }

        public Utf8String(ReadOnlySpan<char> chars) : this(StringHelper.Utf8.GetByteCount(chars))
        {
            StringHelper.Utf8.GetBytes(chars, _byteArray);
        }

        public Utf8String(ReadOnlySpan<byte> bytes) : this(bytes.Length)
        {
            bytes.CopyTo(_byteArray);
        }

        #endregion

        public static Utf8String Create<TState>(
            int length, TState state, SpanAction<byte, TState> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (length == 0)
                return Empty;

            var str = new Utf8String(length);
            action.Invoke(str._byteArray, state);
            return str;
        }

        public static bool IsNullOrEmpty([NotNullWhen(false)] Utf8String? value)
        {
            return value == null || value.Length == 0;
        }

        public Utf8RuneEnumerator EnumerateRunes()
        {
            return new Utf8RuneEnumerator(Bytes);
        }

        public Utf8String Substring(int start, int count)
        {
            if (count == 0)
                return Empty;

            if (start == 0 && count == Length)
                return this;

            if (!IsValidUtf8Slice(_bytes.Span, start, count))
                throw new ArgumentException("The given range would tear UTF8 sequences.");

            ReadOnlyMemory<byte> slice = _bytes.Slice(start, count);
            return new Utf8String(slice);
        }

        public Utf8String Substring(Range range)
        {
            (int offset, int length) = range.GetOffsetAndLength(Length);
            return Substring(offset, length);
        }

        public static bool IsValidUtf8Slice(ReadOnlySpan<byte> span, int start, int count)
        {
            ReadOnlySpan<byte> slice = span.Slice(start, count);

            int sliceStart = start;
            int startOffset = 0;
            for (; startOffset < 3 && sliceStart > 0; startOffset++)
                sliceStart--;

            if (sliceStart == 0 && count == span.Length)
                return true;

            OperationStatus statusOfLast = Rune.DecodeLastFromUtf8(slice, out _, out _);
            if (statusOfLast != OperationStatus.Done)
                return false;

            for (int i = startOffset; i > 0;)
            {
                ReadOnlySpan<byte> preSlice = span[(start - i)..];
                OperationStatus status = Rune.DecodeFromUtf8(preSlice, out _, out int consumed);
                if (status == OperationStatus.Done)
                {
                    ReadOnlySpan<byte> consumedSlice = preSlice.Slice(0, consumed);
                    if (consumedSlice.Overlaps(slice))
                        return false;
                }
                i -= consumed;
            }

            return true;
        }

        public int CompareTo(Utf8String? other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other == null)
                return 1;

            return Bytes.SequenceCompareTo(other.Bytes);
        }

        public bool Equals(Utf8String? other, StringComparison comparison)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other == null)
                return false;

            if (comparison == StringComparison.Ordinal)
                return Bytes.SequenceEqual(other.Bytes);

            return Equals(Bytes, other.Bytes, comparison);
        }

        public bool Equals(Utf8String? other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Utf8String);
        }

        /// <summary>
        /// Constructs a new <see cref="string"/> from this <see cref="Utf8String"/>.
        /// </summary>
        public override string ToString()
        {
            return StringHelper.Utf8.GetString(Bytes);
        }

        public static Utf8String Concat(RuneEnumerator value1, RuneEnumerator value2, RuneEnumerator value3)
        {
            var bytes = new List<byte>();

            foreach (byte b in value1.GetUtf8Enumerator())
                bytes.Add(b);

            foreach (byte b in value2.GetUtf8Enumerator())
                bytes.Add(b);

            foreach (byte b in value3.GetUtf8Enumerator())
                bytes.Add(b);

            return new Utf8String(CollectionsMarshal.AsSpan(bytes));
        }

        public static Utf8String Concat(ReadOnlySpan<byte> value1, ReadOnlySpan<byte> value2, ReadOnlySpan<byte> value3)
        {
            byte[] bytes = new byte[value1.Length + value2.Length + value3.Length];
            Span<byte> dst = bytes.AsSpan();

            value1.CopyTo(dst);
            dst = dst[value1.Length..];

            value2.CopyTo(dst);
            dst = dst[value2.Length..];

            value3.CopyTo(dst);
            dst = dst[value3.Length..];

            return new Utf8String(bytes);
        }

        public static Utf8String Concat(Utf8String? value1, Utf8String? value2, Utf8String? value3)
        {
            return Concat(value1.AsSpan(), value2.AsSpan(), value3.AsSpan());
        }

        public static bool Equals(ReadOnlySpan<byte> firstUtf8, ReadOnlySpan<byte> secondUtf8, StringComparison comparison)
        {
            Span<char> firstUtf16Buf = stackalloc char[32];
            Span<char> secondUtf16Buf = stackalloc char[32];
            do
            {
                var firstStatus = Utf8.ToUtf16(firstUtf8, firstUtf16Buf, out int firstRead, out int firstWritten);
                if (firstStatus != OperationStatus.Done &&
                    firstStatus != OperationStatus.DestinationTooSmall)
                    throw new Exception("Failed to convert UTF-8 to UTF-16.");

                var secondStatus = Utf8.ToUtf16(secondUtf8, secondUtf16Buf, out int secondRead, out int secondWritten);
                if (secondStatus != OperationStatus.Done &&
                    secondStatus != OperationStatus.DestinationTooSmall)
                    throw new Exception("Failed to convert UTF-8 to UTF-16.");

                if (firstWritten > secondWritten)
                    break;

                ReadOnlySpan<char> firstSlice = firstUtf16Buf.Slice(0, firstWritten);
                ReadOnlySpan<char> secondSlice = secondUtf16Buf.Slice(0, secondWritten);
                if (!firstSlice.Equals(secondSlice, comparison))
                    break;

                firstUtf8 = firstUtf8[firstRead..];
                secondUtf8 = secondUtf8[secondRead..];
            }
            while (firstUtf8.Length != secondUtf8.Length);

            return firstUtf8.IsEmpty && secondUtf8.IsEmpty;
        }

        public static bool Equals(ReadOnlySpan<char> utf16, ReadOnlySpan<byte> utf8, StringComparison comparison)
        {
            Span<char> utf16Buf = stackalloc char[32];
            do
            {
                var status = Utf8.ToUtf16(utf8, utf16Buf, out int read, out int written);
                if (status != OperationStatus.Done &&
                    status != OperationStatus.DestinationTooSmall)
                    throw new Exception("Failed to convert UTF-8 to UTF-16.");

                if (written > utf16.Length)
                    break;

                if (!utf16.Slice(0, written).Equals(utf16Buf.Slice(0, written), comparison))
                    break;

                utf16 = utf16[written..];
                utf8 = utf8[read..];
            }
            while (utf8.Length > 0);

            return utf8.IsEmpty;
        }

        // TODO: possibly optimize with interning

        [return: NotNullIfNotNull("value")]
        public static explicit operator string?(Utf8String? value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        [return: NotNullIfNotNull("value")]
        public static explicit operator Utf8String?(string? value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return Empty;

            return new Utf8String(value);
        }

        [return: NotNullIfNotNull("value")]
        public static Utf8String? ToUtf8String(string? value)
        {
            return (Utf8String?)value;
        }

        public override int GetHashCode()
        {
            return LongEqualityComparer<Utf8String>.Default.GetHashCode(this);
        }

        public long GetLongHashCode()
        {
            return LongEqualityComparer<Utf8String>.Default.GetLongHashCode(this);
        }

        public static bool operator ==(Utf8String? left, Utf8String? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Utf8String? left, Utf8String? right)
        {
            return !(left == right);
        }

        public static bool operator <(Utf8String? left, Utf8String? right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Utf8String? left, Utf8String? right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Utf8String? left, Utf8String? right)
        {
            return left is object && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Utf8String? left, Utf8String? right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
