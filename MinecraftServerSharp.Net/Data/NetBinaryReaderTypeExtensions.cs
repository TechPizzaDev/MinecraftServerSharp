using System.Buffers;
using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.Data
{
    public static class NetBinaryReaderTypeExtensions
    {
        public static OperationStatus Read(this NetBinaryReader reader, out Position value)
        {
            var status = reader.Read(out long rawValue);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            value = new Position((ulong)rawValue);
            return OperationStatus.Done;
        }
    }
}
