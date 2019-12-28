using System;
using System.IO;
using System.Text;
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

		#region Write(string)

		public override void Write(string value) => Write(value, NetTextHelper.BigUtf16, true);

		public void Write(Utf8String value) => Write(value.ToString(), NetTextHelper.Utf8, true);

		public void WriteRaw(string value) => Write(value, NetTextHelper.BigUtf16, false);

		public void WriteRaw(Utf8String value) => Write(value.ToString(), NetTextHelper.Utf8, false);

		private void Write(string value, Encoding encoding, bool includeLength)
		{
			if (includeLength)
				Write((VarInt32)value.Length);

			Span<byte> tmp = stackalloc byte[1024];
			int left = encoding.GetByteCount(value);
			int offset = 0;
			while (left > 0)
			{
				int written = encoding.GetBytes(value.AsSpan(offset), tmp);
				Write(tmp.Slice(0, written));

				left -= written;
				offset += written;
			}
		}

		#endregion

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
