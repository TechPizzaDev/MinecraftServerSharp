using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Data
{
    public readonly struct NetBinaryReader
    {
        public Stream BaseStream { get; }

        public long Position => BaseStream.Position;
        public long Length => BaseStream.Length;

        public NetBinaryReader(Stream stream) => BaseStream = stream;

        //private void AssertHasStream()
        //{
        //    if (BaseStream == null)
        //        throw new InvalidOperationException("The underlying stream is null.");
        //}

        public long Seek(int offset, SeekOrigin origin)
        {
            //AssertHasStream();
            return BaseStream.Seek(offset, origin);
        }

        public int TryRead(Span<byte> buffer)
        {
            //AssertHasStream();
            return BaseStream.Read(buffer);
        }

        public int Read()
        {
            //AssertHasStream();
            return BaseStream.ReadByte();
        }

        public bool ReadBool() => ReadByte() != 0;

        public sbyte ReadSByte() => (sbyte)ReadByte();

        public byte ReadByte()
        {
            int value = Read();
            if (value == -1)
                throw new EndOfStreamException();
            return (byte)value;
        }

        public short ReadShort()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            this.Read(buffer);
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        }

        public ushort ReadUShort()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            this.Read(buffer);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public int ReadInt()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            this.Read(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public long ReadLong()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            this.Read(buffer);
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
        }

        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());

        public VarInt ReadVarInt(out int bytes)
        {
            //AssertHasStream();
            VarInt.TryDecode(BaseStream, out var result, out bytes);
            return (VarInt)result;
        }

        public VarInt ReadVarInt() => ReadVarInt(out _);

        public VarLong ReadVarLong()
        {
            //AssertHasStream();
            return VarLong.Decode(BaseStream);
        }

        #region ReadUtf16String

        public string ReadUtf16String()
        {
            int byteCount = ReadVarInt();
            int length = byteCount / sizeof(char);
            return ReadUtf16String(length);
        }

        public string ReadUtf16String(int length)
        {
            NetTextHelper.AssertValidStringByteLength(length * sizeof(char));

            return string.Create(length, this, (output, reader) =>
            {
                // We can use the string as the backing buffer.
                var outputBytes = MemoryMarshal.AsBytes(output);
                reader.Read(outputBytes);

                if (NetTextHelper.BigUtf16.GetChars(outputBytes, output) != output.Length)
                    throw new InvalidDataException();
            });
        }

        #endregion

        #region ReadString

        public Utf8String ReadString()
        {
            int byteCount = ReadVarInt();
            if (byteCount > 3)
                throw new InvalidDataException();

            int length = byteCount / sizeof(byte);
            return ReadString(length);
        }

        public Utf8String ReadString(int length)
        {
            int bytes = length * sizeof(byte);
            NetTextHelper.AssertValidStringByteLength(bytes);

            // TODO: put this under a "UNSAFE" conditional
            unsafe
            {
                return Utf8String.Create(bytes, this, (output, reader) =>
                {
                    reader.Read(output);
                });
            }
        }

        #endregion
    }
}
