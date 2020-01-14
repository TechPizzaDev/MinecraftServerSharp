using System;
using System.Buffers;
using MinecraftServerSharp.Network;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.DataTypes
{
    public unsafe sealed class Utf8String : IDisposable
    {
        public static Utf8String Empty { get; } = new Utf8String(0);

        private readonly UnmanagedPointer<byte> _bytes;

        public bool IsDisposed => _bytes.IsDisposed;

        public int Length => _bytes.Capacity;
        public ReadOnlySpan<byte> Bytes => _bytes.Span;

        #region Constructors

        private Utf8String(int length)
        {
            _bytes = length == 0 ? Empty._bytes : new UnmanagedPointer<byte>(length);
        }

        public Utf8String(string value) : this(NetTextHelper.Utf8.GetByteCount(value))
        {
            NetTextHelper.Utf8.GetBytes(value, _bytes.Span);
        }

        public Utf8String(ReadOnlySpan<byte> bytes) : this(bytes.Length)
        {
            bytes.CopyTo(_bytes.Span);
        }

        #endregion

        public static Utf8String Create<TState>(int length, TState state, SpanAction<byte, TState> action)
        {
            var str = new Utf8String(length);
            action.Invoke(str._bytes.Span, state);
            return str;
        }

        /// <summary>
        /// Constructs a new <see cref="string"/> from this <see cref="Utf8String"/>.
        /// </summary>
        public override string ToString()
        {
            return NetTextHelper.Utf8.GetString(_bytes.Span);
        }

        public void Dispose()
        {
            _bytes.Dispose();
        }
    }
}
