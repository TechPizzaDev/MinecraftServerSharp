using System.IO;

namespace SharpMinecraftServer.Network
{
    public class NetBinaryWriter : BinaryWriter
	{
		public NetBinaryWriter(Stream stream) : base(stream)
		{
		}

		public void WriteVar(int value)
		{
			Write7BitEncodedInt(value);
		}

		public void WriteVar(long value)
		{
			//int count = 1;
			while (value >= 0x80)
			{
				Write((byte)(value | 0x80));
				value >>= 7;
				//count++;
			}
			Write((byte)value);
			//return count;
		}
	}
}
