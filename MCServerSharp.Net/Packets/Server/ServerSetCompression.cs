
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SetCompression)]
    public readonly struct ServerSetCompression
    {
        [DataProperty(0)] public VarInt Threshold { get; }

        public ServerSetCompression(VarInt threshold)
        {
            Threshold = threshold;
        }
    }
}
