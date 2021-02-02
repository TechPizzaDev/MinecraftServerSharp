using System;
using System.Runtime.CompilerServices;
using MCServerSharp.Blocks;
using MCServerSharp.Data.IO;
using MCServerSharp.NBT;
using MCServerSharp.World;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.ChunkData)]
    public struct ServerChunkData : IDataWritable
    {
        public const int ColumnHeight = 16;

        public const int UnderlyingDataSize = 4096;

        public LocalChunkColumn ChunkColumn { get; }
        public int IncludedSectionsMask { get; }
        public bool FullChunk => IncludedSectionsMask == 65535;

        // TODO?: create flash-copying (fast deep-clone) of chunks for serialization purposes,
        // possibly with some kind of locking on end of dimension tick

        public ServerChunkData(LocalChunkColumn chunkColumn, int includedSectionsMask)
        {
            ChunkColumn = chunkColumn;
            IncludedSectionsMask = includedSectionsMask;
        }

        public static int GetSectionMask(LocalChunkColumn chunkColumn)
        {
            if (chunkColumn == null)
                throw new ArgumentNullException(nameof(chunkColumn));

            int sectionMask = 0;
            for (int sectionY = 0; sectionY < ColumnHeight; sectionY++)
            {
                if (chunkColumn.ContainsChunk(sectionY))
                    sectionMask |= 1 << sectionY;
            }
            return sectionMask;
        }

        [SkipLocalsInit]
        public void Write(NetBinaryWriter writer)
        {
            writer.Write(ChunkColumn.Position.X);
            writer.Write(ChunkColumn.Position.Z);
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

            int sectionDataLength = GetChunkSectionDataLength(ChunkColumn);
            writer.WriteVar(sectionDataLength);
            WriteChunk(writer, ChunkColumn);

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

        public static int GetChunkSectionDataLength(LocalChunkColumn chunkColumn)
        {
            if (chunkColumn == null)
                throw new ArgumentNullException(nameof(chunkColumn));

            int length = 0;

            for (int chunkY = 0; chunkY < ColumnHeight; chunkY++)
            {
                if (chunkColumn.TryGetChunk(chunkY, out LocalChunk? chunk))
                    length += GetChunkSectionDataLength(chunk);
            }

            return length;
        }

        public static void WriteChunk(NetBinaryWriter writer, LocalChunkColumn chunkColumn)
        {
            if (chunkColumn == null)
                throw new ArgumentNullException(nameof(chunkColumn));

            for (int chunkY = 0; chunkY < ColumnHeight; chunkY++)
            {
                if (chunkColumn.TryGetChunk(chunkY, out LocalChunk? chunk))
                {
                    LocalChunk.BlockEnumerator blocks = chunk.EnumerateBlocks();
                    WriteBlocks(writer, ref blocks);
                }
            }
        }

        private static int GetUnderlyingDataLength(int size, int bitsPerBlock)
        {
            int blocksPerLong = 64 / bitsPerBlock;
            int longCount = (size + blocksPerLong - 1) / blocksPerLong;
            return longCount;
        }

        public static int GetChunkSectionDataLength(IChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            IBlockPalette palette = chunk.BlockPalette;

            int length = 0;
            length += sizeof(short);
            length += sizeof(byte);
            length += palette.GetEncodedSize();

            int longCount = GetUnderlyingDataLength(UnderlyingDataSize, palette.BitsPerBlock);
            length += VarInt.GetEncodedSize(longCount);
            length += longCount * sizeof(ulong);

            return length;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void WriteBlocks<TBlocks>(
            NetBinaryWriter writer, ref TBlocks blocks)
            where TBlocks : IBlockEnumerator
        {
            IBlockPalette palette = blocks.BlockPalette;
            int bitsPerBlock = palette.BitsPerBlock;
            int dataLength = GetUnderlyingDataLength(UnderlyingDataSize, bitsPerBlock);

            writer.Write((short)LocalChunk.BlockCount);
            writer.Write((byte)bitsPerBlock);
            palette.Write(writer);
            writer.WriteVar(dataLength);

            int blocksPerLong = 64 / bitsPerBlock;
            int valueShift = bitsPerBlock * blocksPerLong - bitsPerBlock;

            // A bitmask that contains bitsPerBlock set bits
            uint valueMask = (uint)((1 << bitsPerBlock) - 1);
            ulong bitBufferMask = 0;
            for (int i = 0; i < blocksPerLong; i++)
            {
                bitBufferMask <<= bitsPerBlock;
                bitBufferMask |= valueMask;
            }

            const int BlockBufferFactor = 4;
            int blockBufferLength = blocksPerLong * BlockBufferFactor;
            uint* blockBufferP = stackalloc uint[blockBufferLength];
            uint* blockBufferP0 = blockBufferP + blocksPerLong * 0;
            uint* blockBufferP1 = blockBufferP + blocksPerLong * 1;
            uint* blockBufferP2 = blockBufferP + blocksPerLong * 2;
            uint* blockBufferP3 = blockBufferP + blocksPerLong * 3;
            Span<uint> blockBuffer = new Span<uint>(blockBufferP, blockBufferLength);

            int dataBufferLength = 512;
            ulong* dataBufferP = stackalloc ulong[dataBufferLength];
            Span<ulong> dataBuffer = new Span<ulong>(dataBufferP, dataBufferLength);

            int dataOffset = 0;
            ulong bitBuffer0 = 0;
            ulong bitBuffer1 = 0;
            ulong bitBuffer2 = 0;
            ulong bitBuffer3 = 0;

            while (blocks.Remaining >= blockBuffer.Length)
            {
                int consumed = blocks.Consume(blockBuffer);
                int bufferCount = consumed / blocksPerLong;

                if (dataOffset + bufferCount >= dataBuffer.Length)
                {
                    writer.Write(dataBuffer.Slice(0, dataOffset));
                    dataOffset = 0;
                }

                switch (bufferCount)
                {
                    case 1:
                        for (int j = 0; j < blocksPerLong; j++)
                        {
                            bitBuffer0 >>= bitsPerBlock;
                            bitBuffer0 |= (ulong)blockBufferP0[j] << valueShift;
                        }
                        dataBufferP[dataOffset++] = bitBuffer0;
                        break;

                    case 2:
                        for (int j = 0; j < blocksPerLong; j++)
                        {
                            bitBuffer0 >>= bitsPerBlock;
                            bitBuffer1 >>= bitsPerBlock;
                            bitBuffer0 |= (ulong)blockBufferP0[j] << valueShift;
                            bitBuffer1 |= (ulong)blockBufferP1[j] << valueShift;
                        }
                        dataBufferP[dataOffset++] = bitBuffer0;
                        dataBufferP[dataOffset++] = bitBuffer1;
                        break;

                    case 3:
                        for (int j = 0; j < blocksPerLong; j++)
                        {
                            bitBuffer0 >>= bitsPerBlock;
                            bitBuffer1 >>= bitsPerBlock;
                            bitBuffer2 >>= bitsPerBlock;
                            bitBuffer0 |= (ulong)blockBufferP0[j] << valueShift;
                            bitBuffer1 |= (ulong)blockBufferP1[j] << valueShift;
                            bitBuffer2 |= (ulong)blockBufferP2[j] << valueShift;
                        }
                        dataBufferP[dataOffset++] = bitBuffer0;
                        dataBufferP[dataOffset++] = bitBuffer1;
                        dataBufferP[dataOffset++] = bitBuffer2;
                        break;

                    case 4:
                        for (int j = 0; j < blocksPerLong; j++)
                        {
                            bitBuffer0 >>= bitsPerBlock;
                            bitBuffer1 >>= bitsPerBlock;
                            bitBuffer2 >>= bitsPerBlock;
                            bitBuffer3 >>= bitsPerBlock;
                            bitBuffer0 |= (ulong)blockBufferP0[j] << valueShift;
                            bitBuffer1 |= (ulong)blockBufferP1[j] << valueShift;
                            bitBuffer2 |= (ulong)blockBufferP2[j] << valueShift;
                            bitBuffer3 |= (ulong)blockBufferP3[j] << valueShift;
                        }
                        dataBufferP[dataOffset++] = bitBuffer0;
                        dataBufferP[dataOffset++] = bitBuffer1;
                        dataBufferP[dataOffset++] = bitBuffer2;
                        dataBufferP[dataOffset++] = bitBuffer3;
                        break;
                }
            }

            while (blocks.Remaining > 0)
            {
                if (dataOffset == dataBuffer.Length)
                {
                    writer.Write(dataBuffer);
                    dataOffset = 0;
                }

                Span<uint> blockSlice = blockBuffer.Slice(0, blocksPerLong);
                int consumed = blocks.Consume(blockSlice);
                blockSlice = blockSlice.Slice(0, consumed);

                ulong bitBuffer = 0;
                for (int j = blockSlice.Length; j-- > 0;)
                {
                    bitBuffer <<= bitsPerBlock;
                    bitBuffer |= blockSlice[j];
                }
                dataBufferP[dataOffset++] = bitBuffer & bitBufferMask;
            }

            writer.Write(dataBuffer.Slice(0, dataOffset));
        }
    }
}
