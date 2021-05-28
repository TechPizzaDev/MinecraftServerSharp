
namespace MCServerSharp.NBT
{
    public struct NbtOptions
    {
        public const int DefaultMaxDepth = 32;

        public static NbtOptions JavaDefault { get; } = new NbtOptions
        {
            IsBigEndian = true,
            IsVarArrayLength = false,
            IsVarStringLength = false,
            MaxDepth = DefaultMaxDepth
        };

        public bool IsBigEndian { get; set; }
        public bool IsVarArrayLength { get; set; }
        public bool IsVarStringLength { get; set; }
        public int MaxDepth { get; set; }
    }
}
