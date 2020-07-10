
namespace MinecraftServerSharp.Network.Packets.Client
{
    [PacketStruct(ClientPacketId.TeleportConfirm)]
    public readonly struct ClientTeleportConfirm
    {
        public VarInt TeleportId { get; }

        [PacketConstructor]
        public ClientTeleportConfirm(VarInt teleportId)
        {
            TeleportId = teleportId;
        }
    }
}
