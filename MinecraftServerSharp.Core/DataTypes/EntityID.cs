
namespace MinecraftServerSharp.DataTypes
{
    public readonly struct EntityID
    {
        public byte X { get; }
        public byte Y { get; }
        public byte Z { get; }
        public byte W { get; }

        public EntityID(byte x, byte y, byte z, byte w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
}
