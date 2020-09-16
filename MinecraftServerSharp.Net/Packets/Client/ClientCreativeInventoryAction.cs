namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.CreativeInventoryAction)]
    public readonly struct ClientCreativeInventoryAction
    {
        public short Slot { get; }
        public Slot SlotData { get; }

        [PacketConstructor]
        public ClientCreativeInventoryAction(short slot, Slot slotData)
        {
            Slot = slot;
            SlotData = slotData;
        }
    }
}
