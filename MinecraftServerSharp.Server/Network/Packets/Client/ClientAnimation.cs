using MinecraftServerSharp.Data;

namespace MinecraftServerSharp.Net.Packets
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
