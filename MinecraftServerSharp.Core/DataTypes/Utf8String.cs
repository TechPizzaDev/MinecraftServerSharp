using System;
using System.Buffers;
using System.Runtime.InteropServices;
using MinecraftServerSharp.Network;

namespace MinecraftServerSharp.DataTypes
{
    public sealed class Utf8String : IDisposable
    {
        public static Utf8String Empty { get; } = new Utf8String(0);

        private IntPtr _bytes;

        public int Length { get; }
        public bool IsDisposed => _bytes == null;

        private unsafe Span<byte> RawBytes => new Span<byte>((void*)_bytes, Length);
        public ReadOnlySpan<byte> Bytes => RawBytes;

        #region Constructors

        private Utf8String(int length)
        {
            _bytes = length == 0 ? Empty._bytes : Marshal.AllocHGlobal(length);
        }

        public Utf8String(string value) : this(NetTextHelper.Utf8.GetByteCount(value))
        {
            NetTextHelper.Utf8.GetBytes(value, RawBytes);
        }

        public Utf8String(ReadOnlySpan<byte> bytes) : this(bytes.Length)
        {
            bytes.CopyTo(RawBytes);
        }

        #endregion

        public static Utf8String Create<TState>(int length, TState state, SpanAction<byte, TState> action)
        {
            var str = new Utf8String(length);
            action.Invoke(str.RawBytes, state);
            return str;
        }

        /// <summary>
        /// Constructs a new <see cref="string"/> from this <see cref="Utf8String"/>.
        /// </summary>
        public override string ToString()
        {
            return NetTextHelper.Utf8.GetString(RawBytes);
        }

        #region IDisposable

        private void DisposeCore()
        {
            if (_bytes != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_bytes);
                _bytes = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        ~Utf8String()
        {
            DisposeCore();
        }

        #endregion
    }
}
