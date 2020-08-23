
namespace MinecraftServerSharp.Data.IO
{
    public struct NetBinaryOptions
    {
        public static NetBinaryOptions JavaDefault => new NetBinaryOptions
        {
            IsBigEndian = true
        };

        public bool IsBigEndian { get; set; }
    }
}