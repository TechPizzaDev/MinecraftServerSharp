using System.IO;
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

        public VarInt32 ReadVarInt32()
        {
            if (!VarInt32.TryDecode(BaseStream, out var result, out _))
                throw new EndOfStreamException();
            return result;
        }

        public VarInt64 ReadVarInt64() => VarInt64.Decode(BaseStream);
	}
}
