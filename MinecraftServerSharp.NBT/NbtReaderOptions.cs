namespace MinecraftServerSharp.NBT
{
    public struct NbtReaderOptions
    {
        public bool IsBigEndian { get; set; }

        public NbtReaderOptions(bool isBigEndian)
        {
            IsBigEndian = isBigEndian;
        }
    }
}
