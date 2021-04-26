using System;
using MCServerSharp.Blocks;

namespace MCServerSharp.World
{
    public partial class LocalChunk
    {
        public struct BlockEnumerator : IBlockEnumerator
        {
            private LocalChunk _chunk;
            private int _offset;

            public IBlockPalette BlockPalette => _chunk.BlockPalette;
            public int Count => BlockCount;
            public int Remaining => BlockCount - _offset;

            public BlockEnumerator(LocalChunk chunk) : this()
            {
                _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            }

            public int Consume(Span<uint> destination)
            {
                int count = _chunk.GetBlockId(_offset, destination);
                _offset += count;
                return count;
            }
        }
    }
}
