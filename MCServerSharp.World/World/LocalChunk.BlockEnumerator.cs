using System;
using MCServerSharp.Blocks;

namespace MCServerSharp.World
{
    public partial class LocalChunk
    {
        public struct BlockEnumerator : IBlockEnumerator
        {
            private LocalChunk _chunk;
            private uint[] _array;
            private int _offset;

            public IBlockPalette BlockPalette => _chunk.BlockPalette;
            public int Count => BlockCount;
            public int Remaining => BlockCount - _offset;

            public BlockEnumerator(LocalChunk chunk) : this()
            {
                _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
                _array = _chunk._blocks;
            }

            public int Consume(Span<uint> destination)
            {
                uint[] array = _array;
                if ((uint)_offset >= (uint)array.Length)
                    return 0;

                int left = array.Length - _offset;
                if (left > destination.Length)
                    left = destination.Length;

                array.AsSpan(_offset, left).CopyTo(destination);
                _offset += left;
                return left;
            }
        }
    }
}
