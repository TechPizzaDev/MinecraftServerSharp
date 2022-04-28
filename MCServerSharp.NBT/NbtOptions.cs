
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
            MaxDepth = DefaultMaxDepth,
            TypeForEmptyList = NbtType.End,
        };

        public bool IsBigEndian { get; set; }
        public bool IsVarArrayLength { get; set; }
        public bool IsVarStringLength { get; set; }
        public int MaxDepth { get; set; }

        /// <summary>
        /// The expected type for empty list tags. 
        /// Can be <see langword="null"/> to skip validation.
        /// </summary>
        public NbtType? TypeForEmptyList { get; set; }
    }
}
