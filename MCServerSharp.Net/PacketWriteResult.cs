
namespace MCServerSharp.Net
{
    public readonly struct PacketWriteResult
    {
        public int DataLength { get; }
        public int? CompressedLength { get; }
        public int TotalLength { get; }

        public PacketWriteResult(
            int dataLength, int? compressedLength, int totalLength)
        {
            DataLength = dataLength;
            CompressedLength = compressedLength;
            TotalLength = totalLength;
        }
    }
}
