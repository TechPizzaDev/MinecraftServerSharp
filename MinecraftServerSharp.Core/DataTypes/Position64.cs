
namespace MinecraftServerSharp.DataTypes
{
    public struct Position64
    {
        private const int Mask26Bit = 0x3ffffff;
        private const int Mask12Bit = 0xfff;

        public ulong Value;

        public Position64(ulong value) => Value = value;

        public int X
        {
            readonly get
            {
                ulong x = Value >> 38;
                if (x >= 1 << 25)
                    x -= 1 << 26;
                return (int)x;
            }
            set
            {
                const long ZYMask = 0x0000003ffffff_fff;
                Value = (Value & ZYMask) | (((ulong)value & Mask26Bit) << 38);
            }
        }

        public int Z
        {
            readonly get
            {
                ulong z = Value << 26 >> 38;
                if (z >= 1 << 25)
                    z -= 1 << 26;
                return (int)z;
            }
            set
            {
                const ulong XYMask = 0xffffffe000000_fff;
                Value = (Value & XYMask) | (((ulong)value & Mask26Bit) << 12);
            }
        }

        public int Y
        {
            readonly get
            {
                ulong y = Value & Mask12Bit;
                if (y >= 1 << 11)
                    y -= 1 << 12;
                return (int)y;
            }
            set
            {
                const ulong XZMask = 0xfffffffffffff_000;
                Value = (Value & XZMask) | ((ulong)value & Mask12Bit);
            }
        }
    }
}
