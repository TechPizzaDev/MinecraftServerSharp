using System;
using System.IO;

namespace SharpMinecraftServer.Network
{
    public partial class NetBuffer
	{
		public int Read() => _reader.Read();
		public int Read(Span<byte> buffer) => _reader.Read(buffer);
		public int Read(Span<char> buffer) => _reader.Read(buffer);
		public bool ReadBoolean() => _reader.ReadBoolean();

		public byte ReadByte() => _reader.ReadByte();
		public byte[] ReadBytes(int count) => _reader.ReadBytes(count);
		public int ReadBytes(int count, Stream output) => _reader.ReadBytes(count, output);

		public char ReadChar() => _reader.ReadChar();
		public char[] ReadChars(int count) => _reader.ReadChars(count);

		public decimal ReadDecimal() => _reader.ReadDecimal();
		public double ReadDouble() => _reader.ReadDouble();
		public short ReadInt16() => _reader.ReadInt16();
		public int ReadInt32() => _reader.ReadInt32();
		public long ReadInt64() => _reader.ReadInt64();
		public sbyte ReadSByte() => _reader.ReadSByte();
		public float ReadSingle() => _reader.ReadSingle();
		public string ReadString() => _reader.ReadString();
		public ushort ReadUInt16() => _reader.ReadUInt16();
		public uint ReadUInt32() => _reader.ReadUInt32();
		public ulong ReadUInt64() => _reader.ReadUInt64();
		public int ReadVarInt32() => _reader.ReadVarInt32();
		public long ReadVarInt64 => _reader.ReadVarInt64();
	}
}
