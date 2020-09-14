using System.Buffers;
using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.NBT
{
    public static class NetBinaryReaderNbtExtensions
    {
        public static OperationStatus Read(this NetBinaryReader reader, out NbtDocument? document)
        {
            // TODO: optimize with pooling and use cached instance for "single End tag" documents

            byte[] nbtData = reader.ReadBytes((int)reader.Remaining);
            document = NbtDocument.Parse(nbtData);

            return OperationStatus.Done;
        }
    }
}
