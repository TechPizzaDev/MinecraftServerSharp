
namespace MCServerSharp.Data.IO
{
    public struct NetBinaryOptions
    {
        public static NetBinaryOptions JavaDefault => new NetBinaryOptions
        {
            IsBigEndian = true,
            UseAvx2Hint = true,
        };

        public bool IsBigEndian { get; set; }

        /// <summary>
        /// Suggest to use AVX2 even if it may not greatly improve performance.
        /// </summary>
        /// <remarks>
        /// AVX2 can be slow on older AMD CPUs.
        /// </remarks>
        public bool UseAvx2Hint { get; set; }
    }
}