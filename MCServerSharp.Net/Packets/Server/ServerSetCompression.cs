
namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.SetCompression)]
    public readonly struct ServerSetCompression
    {
        [PacketProperty(0)] public VarInt Threshold { get; }

        public ServerSetCompression(VarInt threshold)
        {
            Threshold = threshold;
        }
    }
}
