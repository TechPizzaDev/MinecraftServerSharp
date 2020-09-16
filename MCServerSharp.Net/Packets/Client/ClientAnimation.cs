using MCServerSharp.Data;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.Animation)]
    public readonly struct ClientAnimation
    {
        public HandId Hand { get; }

        [PacketConstructor]
        public ClientAnimation(VarInt hand)
        {
            Hand = (HandId)hand.Value;
        }
    }
}
