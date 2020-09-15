using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace MinecraftServerSharp.Data.IO
{
    public readonly struct NetBinaryWriter
    {
        public Stream BaseStream { get; }
        public NetBinaryOptions Options { get; }

        public long Position { get => BaseStream.Position; set => BaseStream.Position = value; }
        public long Length { get => BaseStream.Length; set => BaseStream.SetLength(value); }

        public NetBinaryWriter(Stream stream, NetBinaryOptions? options = default)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            Options = options ?? NetBinaryOptions.JavaDefault;
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

        public void Write(short value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(short)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteInt16BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteInt16LittleEndian(tmp, value);
            Write(tmp);
        }

        public void Write(ushort value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(ushort)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteUInt16BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(tmp, value);
            Write(tmp);
        }

        public void Write(int value)
        {
            Span<byte> tmp = stackalloc byte[sizeof(int)];
            if (Options.IsBigEndian)
                BinaryPrimitives.WriteInt32BigEndian(tmp, value);
            else
                BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
            Write(tmp);
        }

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
            Write(value, StringHelper.BigUtf16, true);
        }

        public void WriteRaw(string? value) // TODO: , bool isBigEndian)
        {
            Write(value, StringHelper.BigUtf16, false);
        }

        // TODO: optimize

        public void Write(Utf8String? value) => Write(value?.ToString(), StringHelper.Utf8, true);

        public void WriteRaw(Utf8String? value) => Write(value?.ToString(), StringHelper.Utf8, false);

        private void Write(string? value, Encoding encoding, bool includeLength)
        {
            value ??= string.Empty;

            int byteCount = encoding.GetByteCount(value);
            if (includeLength)
                Write((VarInt)byteCount);

            int sliceSize = 512;
            int maxBytesPerSlice = encoding.GetMaxByteCount(sliceSize);
            Span<byte> byteBuffer = stackalloc byte[maxBytesPerSlice];

            int charOffset = 0;
            int charsLeft = value.Length;
            while (charsLeft > 0)
            {
                int charsToWrite = Math.Min(charsLeft, sliceSize);
                var charSlice = value.AsSpan(charOffset, charsToWrite);
                int bytesWritten = encoding.GetBytes(charSlice, byteBuffer);
                Write(byteBuffer.Slice(0, bytesWritten));

                charsLeft -= charsToWrite;
                charOffset += charsToWrite;
            }
        }

        #endregion
    }
}