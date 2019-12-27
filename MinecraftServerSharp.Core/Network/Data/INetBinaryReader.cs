using System;
using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Data
{
	public interface INetBinaryReader : ISeekable
	{
		int Read();
		int Read(Span<byte> buffer);
		int Read(Span<char> buffer);
		bool ReadBoolean();
		byte ReadByte();
		char ReadChar();
		decimal ReadDecimal();
		double ReadDouble();
		short ReadInt16();
		int ReadInt32();
		long ReadInt64();
		sbyte ReadSByte();
		float ReadSingle();
		string ReadString();
		ushort ReadUInt16();
		uint ReadUInt32();
		ulong ReadUInt64();

		VarInt32 ReadVarInt32();
		VarInt64 ReadVarInt64();
	}
}