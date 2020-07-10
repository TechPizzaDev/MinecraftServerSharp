using MinecraftServerSharp.Data;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.Animation)]
    public readonly struct ClientAnimation
    {
        public ClientHandId Hand { get; }

        [PacketConstructor]
        public ClientAnimation(VarInt hand)
        {
            Hand = (ClientHandId)hand.Value;
        }
    }
}
