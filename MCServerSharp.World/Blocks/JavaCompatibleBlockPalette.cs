using System;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public readonly struct JavaCompatibleBlockPalette<TPalette> : IBlockPalette
        where TPalette : IBlockPalette
    {
        public TPalette BasePalette { get; }

        public int BitsPerBlock => Math.Max(4, BasePalette.BitsPerBlock);
        public int Count => BasePalette.Count;

        public JavaCompatibleBlockPalette(TPalette palette)
        {
            BasePalette = palette ?? throw new ArgumentNullException(nameof(palette));
        }

        public uint IdForBlock(BlockState state)
        {
            return BasePalette.IdForBlock(state);
        }

        public BlockState BlockForId(uint id)
        {
            return BasePalette.BlockForId(id);
        }

        public void Write(NetBinaryWriter writer)
        {
            BasePalette.Write(writer);
        }

        public int GetEncodedSize()
        {
            return BasePalette.GetEncodedSize();
        }
    }
}
