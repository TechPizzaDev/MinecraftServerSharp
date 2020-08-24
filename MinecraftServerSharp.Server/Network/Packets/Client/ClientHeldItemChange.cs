
namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.HeldItemChange)]
    public readonly struct ClientHeldItemChange
    {
        public short Slot { get; }

        [PacketConstructor]
        public ClientHeldItemChange(short slot)
        {
            Slot = slot;
        }
    }
}
