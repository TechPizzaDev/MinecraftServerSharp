using MinecraftServerSharp.Data;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.UseItem)]
    public readonly struct ClientUseItem
    {
        public HandId Hand { get; }

        public ClientUseItem(VarInt hand)
        {
            Hand = hand.AsEnum<HandId>();
        }
    }
}
