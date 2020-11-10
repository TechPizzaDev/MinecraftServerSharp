using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public class DirectBlockPalette : IBlockPalette
    {
        public BlockState[] _blocks;

        public int BitsPerBlock { get; }
        public int Count => _blocks.Length;

        // TODO: remove this field
        public Dictionary<Identifier, BlockDescription> blockLookup;

        public DirectBlockPalette(BlockState[] blocks)
        {
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));

            BitsPerBlock = (int)Math.Ceiling(Math.Log2(Count));
        }

        [SuppressMessage("Design", "CA1062", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint IdForBlock(BlockState state)
        {
            return state.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
