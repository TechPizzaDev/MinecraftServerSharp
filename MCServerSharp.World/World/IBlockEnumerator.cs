using System;
using MCServerSharp.Blocks;

namespace MCServerSharp.World
{
    public interface IBlockEnumerator
    {
        IBlockPalette BlockPalette { get; }

        int Count { get; }
        int Remaining { get; }

        int Consume(Span<uint> destination);
    }
}
