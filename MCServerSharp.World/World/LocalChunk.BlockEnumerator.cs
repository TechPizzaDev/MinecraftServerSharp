using System;
using MCServerSharp.Blocks;

namespace MCServerSharp.World
{
    public partial class LocalChunk
    {
        public unsafe struct BlockEnumerator : IBlockEnumerator
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
                Span<uint> span = _chunk.GetBlockSpan();
                int offset = _offset;
                if ((uint)offset >= (uint)span.Length)
                    return 0;

                int left = span.Length - offset;
                if (left > destination.Length)
                    left = destination.Length;

                span.Slice(offset, left).CopyTo(destination);
                _offset += left;
                return left;
            }
        }
    }
}
