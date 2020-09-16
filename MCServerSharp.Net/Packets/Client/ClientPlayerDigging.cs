using MCServerSharp.Data;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PlayerDigging)]
    public readonly struct ClientPlayerDigging
    {
        public DiggingStatus Status { get; }
        public Position Location { get; }
        public BlockFace Face { get; }

        [PacketConstructor]
        public ClientPlayerDigging(VarInt status, Position location, byte face)
        {
            Status = status.AsEnum<DiggingStatus>();
            Location = location;
            Face = (BlockFace)face;
        }
    }
}
