using System;
using System.Collections.Generic;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public class DirectBlockPalette : IBlockPalette
    {
        public BlockState[] _blocks;

        public int BitsPerBlock { get; }

        public int Count => _blocks.Length;

        public Dictionary<Identifier, BlockDescription> blockLookup;

        public DirectBlockPalette(BlockState[] blocks)
        {
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));

            BitsPerBlock = (int)Math.Ceiling(Math.Log2(Count));
        }

        public uint IdForBlock(BlockState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            return state.Id;
        }

        public BlockState BlockForId(uint id)
        {
            return _blocks[id];
        }

        public void Write(NetBinaryWriter writer)
        {
        }

        public int GetEncodedSize()
        {
            return 0;
        }
    }
}
