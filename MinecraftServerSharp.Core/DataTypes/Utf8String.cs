using System;
using System.Buffers;
using System.Diagnostics;

namespace MinecraftServerSharp
{
    [DebuggerDisplay("{ToString()}")]
    public readonly struct Utf8String : IComparable<Utf8String>, IEquatable<Utf8String>
    {
        public static Utf8String Empty { get; } = new Utf8String(Array.Empty<byte>());

        private readonly byte[] _bytes;

        public ReadOnlySpan<byte> Bytes => _bytes;
        public int Length => _bytes.Length;

        #region Constructors

        private Utf8String(byte[] bytes) => _bytes = bytes;

        private Utf8String(int length) : this(length == 0 ? Empty._bytes : new byte[length])
        {
        }

        public Utf8String(string value) : this(StringHelper.Utf8.GetByteCount(value))
        {
            StringHelper.Utf8.GetBytes(value, _bytes);
        }

        public Utf8String(ReadOnlySpan<byte> bytes) : this(bytes.Length)
        {
            bytes.CopyTo(_bytes);
        }

        #endregion

        public static Utf8String Create<TState>(
            int length, TState state, SpanAction<byte, TState> action)
        {
            if (length == 0)
                return Empty;

            var str = new Utf8String(length);
            action.Invoke(str._bytes, state);
            return str;
        }

        public int CompareTo(Utf8String other)
        {
            return Bytes.SequenceCompareTo(other.Bytes);
        }

        public bool Equals(Utf8String other)
        {
            return Length == other.Length 
                && Bytes.SequenceEqual(other.Bytes);
        }

        /// <summary>
        /// Constructs a new <see cref="string"/> from this <see cref="Utf8String"/>.
        /// </summary>
        public override string ToString()
        {
            return StringHelper.Utf8.GetString(Bytes);
        }
    }
}
