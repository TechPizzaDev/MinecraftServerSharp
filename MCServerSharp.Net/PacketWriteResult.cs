
namespace MCServerSharp.Net
{
    public readonly struct PacketWriteResult
    {
        public bool Compressed { get; }
        public int DataLength { get; }
        public int TotalLength { get; }

        public PacketWriteResult(bool compressed, int dataLength, int totalLength)
        {
            Compressed = compressed;
            DataLength = dataLength;
            TotalLength = totalLength;
        }
    }
}
