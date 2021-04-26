using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace MCServerSharp.Data.IO
{
    public readonly struct NetBinaryWriter
    {
        public Stream BaseStream { get; }
        public NetBinaryOptions Options { get; }

        public long Position { get => BaseStream.Position; set => BaseStream.Position = value; }
        public long Length { get => BaseStream.Length; set => BaseStream.SetLength(value); }

        public NetBinaryWriter(Stream stream, NetBinaryOptions options)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            Options = options;
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            BaseStream.Write(buffer);
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        public void Write(byte value)
        {
            BaseStream.WriteByte(value);
        }

        [SkipLocalsInit]
        public void Write(short value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteInt16BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteInt16LittleEndian(tmp, value);
            Write(tmp);
        }

        [SkipLocalsInit]
        public void Write(ushort value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteUInt16BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(tmp, value);
            Write(tmp);
        }

        [SkipLocalsInit]
        public void Write(int value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteInt32BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
            Write(tmp);
        }

        [SkipLocalsInit]
        public void Write(long value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(long)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteInt64BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteInt64LittleEndian(tmp, value);
            Write(tmp);
        }





        // TODO: make into extensions

        [SkipLocalsInit]
        public void Write(VarInt value)
        {
            Span<byte> tmp = stackalloc byte[VarInt.MaxEncodedSize];
            int count = value.Encode(tmp);
            Write(tmp.Slice(0, count));
        }

        public void WriteVar(int value)
        {
            Write((VarInt)value);
        }

        public void WriteVar(uint value)
        {
            Write((VarInt)(int)value);
        }

        [SkipLocalsInit]
        public void Write(VarLong value)
        {
            Span<byte> tmp = stackalloc byte[VarLong.MaxEncodedSize];
            int count = value.Encode(tmp);
            Write(tmp.Slice(0, count));
        }

        public void WriteVar(long value)
        {
            Write((VarLong)value);
        }

        public void WriteVar(ulong value)
        {
            Write((VarLong)(long)value);
        }

        public void Write(float value)
        {
            Write(BitConverter.SingleToInt32Bits(value));
        }

        public void Write(double value)
        {
            Write(BitConverter.DoubleToInt64Bits(value));
        }

        #region String Write

        public void Write(string? value) // TODO: , bool isBigEndian)
        {
            WriteString(value, StringHelper.BigUtf16);
        }

        public void WriteRaw(string? value) // TODO: , bool isBigEndian)
        {
            WriteStringRaw(value, StringHelper.BigUtf16);
        }

        // TODO: optimize

        public void Write(Utf8String? value)
        {
            // TODO: optimize/remove alloc
            WriteString(value?.ToString(), StringHelper.Utf8);
        }

        public void WriteRaw(Utf8String? value)
        {
            // TODO: optimize/remove alloc
            WriteStringRaw(value?.ToString(), StringHelper.Utf8);
        }

        public void Write(Utf8Memory value)
        {
            // TODO: optimize/remove alloc
            WriteString(value.ToString(), StringHelper.Utf8);
        }

        public void WriteRaw(Utf8Memory value)
        {
            // TODO: optimize/remove alloc
            WriteStringRaw(value.ToString(), StringHelper.Utf8);
        }

        public void WriteUtf8(ReadOnlySpan<char> value)
        {
            WriteString(value, StringHelper.Utf8);
        }

        public void WriteRawUtf8(ReadOnlySpan<char> value)
        {
            WriteStringRaw(value, StringHelper.Utf8);
        }

        public void WriteUtf8(ReadOnlyMemory<char> value)
        {
            WriteUtf8(value.Span);
        }

        public void WriteRawUtf8(ReadOnlyMemory<char> value)
        {
            WriteRawUtf8(value.Span);
        }

        public void WriteUtf8(string? value)
        {
            WriteUtf8((ReadOnlySpan<char>)value);
        }

        public void WriteRawUtf8(string? value)
        {
            WriteRawUtf8((ReadOnlySpan<char>)value);
        }

        private void WriteString(ReadOnlySpan<char> value, Encoding encoding)
        {
            if (value == null)
            {
                Write((VarInt)0);
                return;
            }

            int byteCount = encoding.GetByteCount(value);
            Write((VarInt)byteCount);

            WriteStringRaw(value, encoding);
        }

        [SkipLocalsInit]
        private void WriteStringRaw(ReadOnlySpan<char> value, Encoding encoding)
        {
            if (value.IsEmpty)
                return;

            int sliceSize = 512;
            int maxBytesPerSlice = encoding.GetMaxByteCount(sliceSize);
            Span<byte> byteBuffer = stackalloc byte[maxBytesPerSlice];

            int charOffset = 0;
            int charsLeft = value.Length;
            while (charsLeft > 0)
            {
                int charsToWrite = Math.Min(charsLeft, sliceSize);
                ReadOnlySpan<char> charSlice = value.Slice(charOffset, charsToWrite);
                int bytesWritten = encoding.GetBytes(charSlice, byteBuffer);
                Write(byteBuffer.Slice(0, bytesWritten));

                charsLeft -= charsToWrite;
                charOffset += charsToWrite;
            }
        }

        #endregion
    }
}