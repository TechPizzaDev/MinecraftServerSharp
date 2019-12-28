using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Data
{
    public class NetBinaryReader : BinaryReader, INetBinaryReader
    {
        public long Position => BaseStream.Position;
        public long Length => BaseStream.Length;

        public NetBinaryReader(Stream stream) : base(stream)
		{
        }

        public long Seek(int offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

        public override string ReadString()
        {
            int length = ReadVarInt32();
            return ReadString(length);
        }

        public string ReadString(int length)
        {
            return ReadString(length * sizeof(char), NetTextHelper.BigUtf16);
        }

        public Utf8String ReadUtf8String()
        {
            int length = ReadVarInt32();
            return ReadUtf8String(length);
        }

        public Utf8String ReadUtf8String(int length)
        {
            return new Utf8String(ReadString(length, NetTextHelper.Utf8));
        }

        private unsafe string ReadString(int length, Encoding encoding)
        {
            Span<byte> tmp = length <= 1024 ? stackalloc byte[length] : new byte[length];
            if (Read(tmp) != length)
                throw new EndOfStreamException();

            fixed (byte* tmpPtr = &MemoryMarshal.GetReference(tmp))
            {
                return new string((sbyte*)tmpPtr, 0, tmp.Length, encoding);
            }
        }

        public VarInt32 ReadVarInt32()
        {
            if (!VarInt32.TryDecode(BaseStream, out var result, out _))
                throw new EndOfStreamException();
            return result;
        }

        public VarInt64 ReadVarInt64() => VarInt64.Decode(BaseStream);
	}
}
