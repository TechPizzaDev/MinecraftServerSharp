using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using MCServerSharp.Blocks;
using MCServerSharp.Data.IO;
using MCServerSharp.NBT;
using MCServerSharp.World;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.ChunkData)]
    public struct ServerChunkData : IDataWritable
    {
        public const int UnderlyingDataSize = 4096;

        public Chunk Chunk { get; }
        public int IncludedSectionsMask { get; }
        public bool FullChunk => IncludedSectionsMask == 65535;

        // TODO: create flash-copying (fast deep-clone) of chunks for serialization purposes

        public ServerChunkData(Chunk chunk, int includedSectionsMask)
        {
            Chunk = chunk;
            IncludedSectionsMask = includedSectionsMask;
        }

        public void Write(NetBinaryWriter writer)
        {
            writer.Write(Chunk.X);
            writer.Write(Chunk.Z);
            writer.Write(FullChunk);
            writer.WriteVar(IncludedSectionsMask);

            var motionBlocking = new NbtLongArray(36);
            writer.Write(motionBlocking.AsCompound((Utf8String)"Heightmaps", (Utf8String)"MOTION_BLOCKING"));

            if (FullChunk)
            {
                Span<int> biomes = stackalloc int[1024];
                biomes.Fill(1); // 1=plains (defined in ServerMain)

                // TODO: optimize by creating WriteVar for Span
                writer.WriteVar(biomes.Length);
                writer.WriteVar(biomes);
            }

            int sectionDataLength = GetChunkSectionDataLength(Chunk);
            writer.WriteVar(sectionDataLength);
            WriteChunk(writer, Chunk);

            // If you don't support block entities yet, use 0
            // If you need to implement it by sending block entities later with the update block entity packet,
            // do it that way and send 0 as well.  (Note that 1.10.1 (not 1.10 or 1.10.2) will not accept that)
            writer.WriteVar(0);

            //WriteVarInt(data, chunk.BlockEntities.Length);
            //foreach (CompoundTag tag in chunk.BlockEntities)
            //{
            //    WriteCompoundTag(data, tag);
            //}
        }

        public static int GetChunkSectionDataLength(Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            int length = 0;

            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                var section = chunk[sectionY];
                if (section != null)
                    length += GetChunkSectionDataLength(section);
            }

            return length;
        }

        public static void WriteChunk(NetBinaryWriter writer, Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                var section = chunk[sectionY];
                if (section != null)
                {
                    ReadOnlySpan<BlockState> blocks = section.Blocks.Span;
                    WriteBlocks(writer, blocks, section.BlockPalette);
                }
            }
        }

        private static int GetUnderlyingDataLength(int size, int bitsPerBlock)
        {
            int blocksPerLong = 64 / bitsPerBlock;
            int longCount = (size + blocksPerLong - 1) / blocksPerLong;
            return longCount;
        }

        public static int GetChunkSectionDataLength(ChunkSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            IBlockPalette palette = section.BlockPalette;

            int length = 0;
            length += sizeof(short);
            length += sizeof(byte);
            length += palette.GetEncodedSize();

            int longCount = GetUnderlyingDataLength(UnderlyingDataSize, palette.BitsPerBlock);
            length += VarInt.GetEncodedSize(longCount);
            length += longCount * sizeof(ulong);

            return length;
        }

        public static void WriteBlocks(NetBinaryWriter writer, ReadOnlySpan<BlockState> blocks, IBlockPalette palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            // Creating these struct abstractions allow the JIT to inline IdForBlock
            // which yields almost 2x performance. These savings measure up quickly as
            // the function is called millions of times when loading chunks.

            if (palette is DirectBlockPalette directPalette)
                WriteBlocks(writer, blocks, new DirectBlockPaletteWrapper(directPalette));
            else if (palette is IndirectBlockPalette indirectPalette)
                WriteBlocks(writer, blocks, new IndirectBlockPaletteWrapper(indirectPalette));
            else
                WriteBlocks(writer, blocks, palette);
        }

        // TODO: skiplocalsinit
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void WriteBlocks<TPalette>(
            NetBinaryWriter writer, ReadOnlySpan<BlockState> blocks, TPalette palette)
            where TPalette : IBlockPalette
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            int bitsPerBlock = palette.BitsPerBlock;
            int dataLength = GetUnderlyingDataLength(UnderlyingDataSize, bitsPerBlock);

            writer.Write((short)ChunkSection.BlockCount);
            writer.Write((byte)bitsPerBlock);
            palette.Write(writer);
            writer.WriteVar(dataLength);

            // A bitmask that contains bitsPerBlock set bits
            uint valueMask = (uint)((1 << bitsPerBlock) - 1);
            int blocksPerLong = 64 / bitsPerBlock;

            ulong bitBufferMask = 0;
            for (int i = 0; i < blocksPerLong; i++)
            {
                bitBufferMask <<= bitsPerBlock;
                bitBufferMask |= valueMask;
            }

            Span<ulong> buffer = stackalloc ulong[512];
            int bufferOffset = 0;

            while (blocks.Length >= blocksPerLong)
            {
                if (bufferOffset >= buffer.Length)
                {
                    writer.Write(buffer.Slice(0, bufferOffset));
                    bufferOffset = 0;
                }

                ulong bitBuffer = 0;
                for (int j = 0; j < blocksPerLong; j++)
                {
                    uint value = palette.IdForBlock(blocks[j]);
                    bitBuffer <<= bitsPerBlock;
                    bitBuffer |= value;
                }

                buffer[bufferOffset++] = bitBuffer & bitBufferMask;
                blocks = blocks.Slice(blocksPerLong);
            }

            for (int i = 0; i < blocks.Length;)
            {
                if (bufferOffset == buffer.Length)
                {
                    writer.Write(buffer);
                    bufferOffset = 0;
                }

                ulong bitBuffer = 0;
                int count = Math.Min(blocks.Length, blocksPerLong);
                for (int j = 0; j < count; j++)
                {
                    uint value = palette.IdForBlock(blocks[i++]);
                    bitBuffer <<= bitsPerBlock;
                    bitBuffer |= value;
                }
                buffer[bufferOffset++] = bitBuffer & bitBufferMask;
            }

            writer.Write(buffer.Slice(0, bufferOffset));
        }

        public readonly struct DirectBlockPaletteWrapper : IBlockPalette
        {
            public DirectBlockPalette Palette { get; }

            public int BitsPerBlock => Palette.BitsPerBlock;
            public int Count => Palette.Count;

            public DirectBlockPaletteWrapper(DirectBlockPalette palette)
            {
                Palette = palette ?? throw new ArgumentNullException(nameof(palette));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint IdForBlock(BlockState state) => Palette.IdForBlock(state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BlockState BlockForId(uint id) => Palette.BlockForId(id);

            public int GetEncodedSize() => Palette.GetEncodedSize();

            public void Write(NetBinaryWriter writer) => Palette.Write(writer);
        }

        public readonly struct IndirectBlockPaletteWrapper : IBlockPalette
        {
            public IndirectBlockPalette Palette { get; }

            public int BitsPerBlock => Palette.BitsPerBlock;
            public int Count => Palette.Count;

            public IndirectBlockPaletteWrapper(IndirectBlockPalette palette)
            {
                Palette = palette ?? throw new ArgumentNullException(nameof(palette));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint IdForBlock(BlockState state) => Palette.IdForBlock(state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BlockState BlockForId(uint id) => Palette.BlockForId(id);

            public int GetEncodedSize() => Palette.GetEncodedSize();

            public void Write(NetBinaryWriter writer) => Palette.Write(writer);
        }
    }
}
