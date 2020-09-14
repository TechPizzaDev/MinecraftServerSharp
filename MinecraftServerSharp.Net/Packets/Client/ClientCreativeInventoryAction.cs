using MinecraftServerSharp.NBT;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.CreativeInventoryAction)]
    public readonly struct ClientCreativeInventoryAction
    {
        public bool Present { get; }
        public VarInt ItemID { get; }
        public byte ItemCount { get; }
        public NbtDocument? NBT { get; }

        [PacketConstructor]
        public ClientCreativeInventoryAction(
            [PacketSwitchCase(false)] bool present) : this()
        {
            Present = present;
        }

        // TODO:
        //[PacketConstructor]
        public ClientCreativeInventoryAction(
            [PacketSwitchCase(true)] bool present,
            VarInt itemID,
            byte itemCount,
            NbtDocument nbt)
        {
            Present = present;
            ItemID = itemID;
            ItemCount = itemCount;
            NBT = nbt;
        }
    }
}
