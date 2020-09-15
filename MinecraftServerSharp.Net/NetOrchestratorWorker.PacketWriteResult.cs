namespace MinecraftServerSharp.Net
{
    public partial class NetOrchestratorWorker
    {
        public readonly struct PacketWriteResult
        {
            public bool Compressed { get; }
            public int RawLength { get; }
            public int Length { get; }

            public PacketWriteResult(bool compressed, int rawLength, int length)
            {
                Compressed = compressed;
                Length = length;
                RawLength = rawLength;
            }
        }
    }
}
