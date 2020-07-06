
namespace MinecraftServerSharp.NBT
{
    public struct NbtDocumentOptions
    {
        public const int DefaultMaxDepth = 64;

        public bool IsBigEndian { get; set; }

        public NbtReaderOptions GetReaderOptions()
        {
            return new NbtReaderOptions
            {
                IsBigEndian = IsBigEndian
            };
        }
    }
}
