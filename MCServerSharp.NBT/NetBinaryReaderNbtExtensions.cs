using System.Buffers;
using System.IO;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public static class NetBinaryReaderNbtExtensions
    {
        public static OperationStatus Read(this NetBinaryReader reader, out NbtDocument? document)
        {
            // TODO: 
            // optimize with pooling,
            // read a smarter amount/implement NbtDocument.Parse(Stream)
            // use cached instance for "single End tag" documents

            int toRead = (int)reader.Remaining;
            byte[] nbtData = reader.ReadBytes(toRead);
            document = NbtDocument.Parse(nbtData, out int bytesConsumed);

            int seekBack = bytesConsumed - toRead;
            reader.Seek(seekBack, SeekOrigin.Current);

            return OperationStatus.Done;
        }
    }
}
