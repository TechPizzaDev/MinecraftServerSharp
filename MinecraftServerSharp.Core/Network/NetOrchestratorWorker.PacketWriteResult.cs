namespace MinecraftServerSharp.Network
{
    public partial class NetOrchestratorWorker
    {
        private readonly struct PacketWriteResult
        {
            public static PacketWriteResult Failed { get; } = new PacketWriteResult(false, false, 0, 0);

            public bool Success { get; }
            public bool Compressed { get; }
            public int DataLength { get; }
            public int Length { get; }

            public PacketWriteResult(bool success, bool compressed, int dataLength, int length)
            {
                Success = success;
                Compressed = compressed;
                Length = length;
                DataLength = dataLength;
            }
        }
    }
}
