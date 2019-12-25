using System;
using System.IO;
using SharpMinecraftServer.Utility;

namespace SharpMinecraftServer.Network
{
    public class NetBinaryReader : BinaryReader
	{
		public NetBinaryReader(Stream stream) : base(stream)
		{
		}

        public int ReadBytes(int count, Stream output)
        {
            byte[] buffer = RecyclableMemoryManager.Default.GetBlock();
            try
            {
                int numRead = 0;
                do
                {
                    int n = Read(buffer, 0, count);
                    if (n == 0)
                        break;

                    output.Write(buffer, 0, n);
                    numRead += n;
                    count -= n;
                } while (count > 0);

                return numRead;
            }
            finally
            {
                RecyclableMemoryManager.Default.ReturnBlock(buffer);
            }
        }

        public int ReadVarInt32() => Read7BitEncodedInt();

		public long ReadVarInt64()
        {
            long count = 0;
            int shift = 0;
            long b;
            do
            {
                if (shift > 10 * 7)
                    throw new FormatException("Shift is too big.");

                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;

            } while ((b & 0x80) != 0);

            return count;
        }
	}
}
