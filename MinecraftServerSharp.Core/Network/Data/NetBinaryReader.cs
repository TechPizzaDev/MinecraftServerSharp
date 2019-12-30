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

        public long Seek(int offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

        public int TryRead(Span<byte> buffer) => BaseStream.Read(buffer);

        public int Read() => BaseStream.ReadByte();

        public bool ReadBoolean() => ReadByte() != 0;

        public sbyte ReadSByte() => (sbyte)ReadByte();

        public byte ReadByte()
        {
            int value = Read();
            if (value == -1)
                throw new EndOfStreamException();
            return (byte)value;
        }

        public short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            this.Read(buffer);
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        }

        public ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            this.Read(buffer);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            this.Read(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            this.Read(buffer);
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
        }

        public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());

        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

        public VarInt32 ReadVarInt32()
        {
            if (!VarInt32.TryDecode(BaseStream, out var result, out _))
                throw new EndOfStreamException();
            return result;
        }

        public VarInt64 ReadVarInt64() => VarInt64.Decode(BaseStream);

        #region ReadString

        public string ReadString()
        {
            int byteCount = ReadVarInt32();
            int length = byteCount / sizeof(char);
            return ReadString(length);
        }

        public string ReadString(int length)
        {
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

        #region ReadUtf8String

        public Utf8String ReadUtf8String()
        {
            int byteCount = ReadVarInt32();
            int length = byteCount / sizeof(byte);
            return ReadUtf8String(length);
        }

        public Utf8String ReadUtf8String(int length)
        {
            if (length < 2048)
            {
                // TODO: put this under a "UNSAFE" conditional
                unsafe
                {
                    byte* tmpPtr = stackalloc byte[length];
                    var tmp = new Span<byte>(tmpPtr, length);
                    this.Read(tmp);

                    int charCount = NetTextHelper.Utf8.GetCharCount(tmp);
                    var str = string.Create(charCount, new BytePointer(tmpPtr, length), (output, data) =>
                    {
                        if (NetTextHelper.Utf8.GetChars(data.AsSpan(), output) != output.Length)
                            throw new InvalidDataException();
                    });
                    return new Utf8String(str);
                }
            }
            else
            {
                var tmp = new byte[length];
                this.Read(tmp);

                int charCount = NetTextHelper.Utf8.GetCharCount(tmp);
                var str = string.Create(charCount, tmp, (output, data) =>
                {
                    if (NetTextHelper.Utf8.GetChars(data.AsSpan(), output) != output.Length)
                        throw new InvalidDataException();
                });
                return new Utf8String(str);
            }
        }

        #endregion
    }
}
