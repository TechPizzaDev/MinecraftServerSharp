using MCServerSharp.Data;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.ClickWindow)]
    public readonly struct ClientClickWindow
    {
        public byte WindowID { get; }
        public short Slot { get; }
        public sbyte Button { get; }
        public short ActionNumber { get; }
        public ClickMode Mode { get; }
        public Slot ClickedItem { get; }

        [PacketConstructor]
        public ClientClickWindow(
            byte windowID,
            short slot,
            sbyte button, 
            short actionNumber, 
            VarInt mode, 
            Slot clickedItem)
        {
            WindowID = windowID;
            Slot = slot;
            Button = button;
            ActionNumber = actionNumber;
            Mode = mode.AsEnum<ClickMode>();
            ClickedItem = clickedItem;
        }
    }
}
