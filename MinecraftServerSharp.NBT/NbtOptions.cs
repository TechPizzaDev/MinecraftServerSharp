
namespace MinecraftServerSharp.NBT
{
    public struct NbtOptions
    {
        public const int DefaultMaxDepth = 16;

        public static NbtOptions JavaDefault { get; } = new NbtOptions
        {
            IsBigEndian = true,
            IsVarInt = false,
            MaxDepth = DefaultMaxDepth
        };

        public bool IsBigEndian { get; set; }
        public bool IsVarInt { get; set; }
        public int MaxDepth { get; set; }
    }
}
