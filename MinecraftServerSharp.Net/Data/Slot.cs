using MinecraftServerSharp.NBT;

namespace MinecraftServerSharp.Net.Packets
{
    public readonly struct Slot
    {
        public static Slot Empty => default;

        public bool Present { get; }
        public VarInt ItemID { get; }
        public byte ItemCount { get; }
        public NbtDocument? NBT { get; }

        public Slot(VarInt itemID, byte itemCount, NbtDocument? nbt)
        {
            Present = true;
            ItemID = itemID;
            ItemCount = itemCount;
            NBT = nbt;
        }
    }
}
