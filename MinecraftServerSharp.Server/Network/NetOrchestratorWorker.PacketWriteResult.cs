namespace MinecraftServerSharp.Net
{
    public partial class NetOrchestratorWorker
    {
        public readonly struct PacketWriteResult
        {
            public bool Compressed { get; }
            public int DataLength { get; }
            public int Length { get; }

            public PacketWriteResult(bool compressed, int dataLength, int length)
            {
                Compressed = compressed;
                Length = length;
                DataLength = dataLength;
            }
        }
    }
}
