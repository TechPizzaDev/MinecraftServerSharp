using System;

namespace MinecraftServerSharp.Data
{
	public interface INetBinaryReader : ISeekable
	{
		int Read();
		int TryRead(Span<byte> buffer);
		bool ReadBoolean();
		sbyte ReadSByte();
		byte ReadByte();
		short ReadInt16();
		ushort ReadUInt16();
		int ReadInt32();
		long ReadInt64();

		int ReadVarInt32();
		long ReadVarInt64();

		float ReadSingle();
		double ReadDouble();

		string ReadString();
		string ReadString(int length);
		Utf8String ReadUtf8String();
		Utf8String ReadUtf8String(int length);
	}
}