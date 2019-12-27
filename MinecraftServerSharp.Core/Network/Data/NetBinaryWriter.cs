using System;
using System.IO;
using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Data
{
    public class NetBinaryWriter : BinaryWriter, INetBinaryWriter
	{
		public long Position => BaseStream.Position;
		public long Length => BaseStream.Length;

		public NetBinaryWriter(Stream stream) : base(stream)
		{
		}

		public void WriteVar(VarInt32 value)
		{
			Span<byte> tmp = stackalloc byte[VarInt32.MaxEncodedSize];
			int count = value.Encode(tmp);
			Write(tmp.Slice(0, count));
		}

		public void WriteVar(VarInt64 value)
		{
			Span<byte> tmp = stackalloc byte[VarInt64.MaxEncodedSize];
			int count = value.Encode(tmp);
			Write(tmp.Slice(0, count));
		}
	}
}
