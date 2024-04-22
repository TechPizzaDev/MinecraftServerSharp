using System;
using System.Diagnostics;
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
    public readonly struct ServerChunkData : IDataWritable
    {
        public LocalChunkColumn ChunkColumn { get; }
        public LightUpdate Light { get; }

        // TODO?: create flash-copying (fast deep-clone) of chunks for serialization purposes,
        // possibly with some kind of locking on end of dimension tick

        public ServerChunkData(LocalChunkColumn chunkColumn, LightUpdate light)
        {
            ChunkColumn = chunkColumn;
            Light = light;
        }

        [SkipLocalsInit]
        public void WriteTo(NetBinaryWriter writer)
        {
            writer.Write(ChunkColumn.Position.X);
            writer.Write(ChunkColumn.Position.Z);
            //writer.Write(FullChunk);
            //writer.WriteVar(IncludedSectionsMask);

            var heightmaps = new NbtMutLongArray(37);
            var motionBlocking = heightmaps.ToCompound((Utf8String)"Heightmaps", (Utf8String)"MOTION_BLOCKING");
            writer.Write(motionBlocking);

            //if (FullChunk)
            //{
            //    Span<int> biomes = stackalloc int[1024];
            //    biomes.Fill(1); // 1=plains (defined in ServerMain)
            //
            //    writer.WriteVar(biomes.Length);
            //    writer.WriteVar(biomes);
            //}

            IChunk[] chunkBuffer = new IChunk[ChunkColumn.GetMaxChunkCount()];
            ChunkColumn.TryGetChunks(chunkBuffer);

            int sectionDataLength = GetChunkColumnDataLength(chunkBuffer);
            writer.WriteVar(sectionDataLength);
            WriteChunk(writer, chunkBuffer);

            // If you don't support block entities yet, use 0
            // If you need to implement it by sending block entities later with the update block entity packet,
            // do it that way and send 0 as well.  (Note that 1.10.1 (not 1.10 or 1.10.2) will not accept that)
            writer.WriteVar(0);

            //WriteVarInt(data, chunk.BlockEntities.Length);
            //foreach (CompoundTag tag in chunk.BlockEntities)
            //{
            //    WriteCompoundTag(data, tag);
            //}

            WriteLight(writer);

            Light.Dispose();
        }

        private void WriteLight(NetBinaryWriter writer)
        {
            writer.Write(Light.TrustEdges);
            writer.Write(Light.SkyLightMask);
            writer.Write(Light.BlockLightMask);
            writer.Write(Light.EmptySkyLightMask);
            writer.Write(Light.EmptyBlockLightMask);

            var skyLightArrays = Light.SkyLightArrays;
            writer.WriteVar(skyLightArrays.Count);
            for (int i = 0; i < skyLightArrays.Count; i++)
            {
                writer.WriteVar(2048);
                writer.Write(skyLightArrays[i].Data);
            }

            var blockLightArrays = Light.BlockLightArrays;
            writer.WriteVar(blockLightArrays.Count);
            for (int i = 0; i < blockLightArrays.Count; i++)
            {
                writer.WriteVar(2048);
                writer.Write(blockLightArrays[i].Data);
            }
        }

        public static int GetChunkColumnDataLength(ReadOnlySpan<IChunk?> chunks)
        {
            int length = 0;

            for (int i = 0; i < chunks.Length; i++)
            {
                LocalChunk? chunk = chunks[i] as LocalChunk;

                if (chunk == null || chunk.IsEmpty)
                {
                    length += sizeof(short); // block count

                    // Write single-value block palette
                    length += 1; // Bits per entry
                    length += VarInt.GetEncodedSize(0); // value palette
                    length += VarInt.GetEncodedSize(0); // data array length

                    // Write single-value biome palette
                    length += 1; // Bits per entry
                    length += VarInt.GetEncodedSize(0); // value palette
                    length += VarInt.GetEncodedSize(0); // data array length
                    continue;
                }

                JavaCompatibleBlockPalette<IBlockPalette> javaPalette = new(chunk.BlockPalette);
                length += GetChunkDataLength(javaPalette);

                // Write single-value biome palette
                length += 1; // Bits per entry
                length += VarInt.GetEncodedSize(0); // value palette
                length += VarInt.GetEncodedSize(0); // data array length
            }

            return length;
        }

        public static void WriteChunk(NetBinaryWriter writer, ReadOnlySpan<IChunk?> chunks)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                LocalChunk? chunk = chunks[i] as LocalChunk;

                if (chunk == null || chunk.IsEmpty)
                {
                    writer.Write((short)0);

                    // Write single-value block palette
                    writer.Write((byte)0); // bits per entry
                    writer.WriteVar(0); // value 
                    writer.WriteVar(0); // data array length

                    // Write single-value biome palette
                    writer.Write((byte)0);
                    writer.WriteVar(0);
                    writer.WriteVar(0);
                    continue;
                }

                LocalChunk.BlockEnumerator blocks = chunk.EnumerateBlocks();

                JavaCompatibleBlockPalette<IBlockPalette> javaPalette = new(blocks.BlockPalette);
                WritePalette(writer, javaPalette);
                WriteBlocks(writer, ref blocks, javaPalette.BitsPerBlock);

                // Write single-value biome palette
                writer.Write((byte)0); // bits per entry
                writer.WriteVar(0); // value 
                writer.WriteVar(0); // data array length
            }
        }

        private static int GetUnderlyingDataLength(int size, int bitsPerBlock)
        {
            int blocksPerLong = 64 / bitsPerBlock;
            int longCount = (size + blocksPerLong - 1) / blocksPerLong;
            return longCount;
        }

        public static int GetChunkDataLength<TPalette>(TPalette palette)
            where TPalette : IBlockPalette
        {
            int length = 0;
            length += sizeof(short); // block count
            length += sizeof(byte); // bits per entry
            length += palette.GetEncodedSize();

            int longCount = GetUnderlyingDataLength(LocalChunk.BlockCount, palette.BitsPerBlock);
            length += VarInt.GetEncodedSize(longCount); // data array length
            length += longCount * sizeof(ulong); // data array

            return length;
        }

        public static void WritePalette<TPalette>(NetBinaryWriter writer, TPalette palette)
            where TPalette : IBlockPalette
        {
            int bitsPerBlock = palette.BitsPerBlock;
            int dataLength = GetUnderlyingDataLength(LocalChunk.BlockCount, bitsPerBlock);

            writer.Write((short)LocalChunk.BlockCount);
            writer.Write((byte)bitsPerBlock);
            palette.Write(writer);
            writer.WriteVar(dataLength);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void WriteBlocks<TBlocks>(
            NetBinaryWriter writer, ref TBlocks blocks, int bitsPerBlock)
            where TBlocks : IBlockEnumerator
        {
            int blocksPerLong = 64 / bitsPerBlock;
            int valueShift = bitsPerBlock * blocksPerLong - bitsPerBlock;

            uint* blockBufferP = stackalloc uint[blocksPerLong * 8];
            uint* blockBufferP0 = blockBufferP + blocksPerLong * 0;
            uint* blockBufferP1 = blockBufferP + blocksPerLong * 1;
            uint* blockBufferP2 = blockBufferP + blocksPerLong * 2;
            uint* blockBufferP3 = blockBufferP + blocksPerLong * 3;
            uint* blockBufferP4 = blockBufferP + blocksPerLong * 4;
            uint* blockBufferP5 = blockBufferP + blocksPerLong * 5;
            uint* blockBufferP6 = blockBufferP + blocksPerLong * 6;
            uint* blockBufferP7 = blockBufferP + blocksPerLong * 7;
            Span<uint> fullBlockBuffer = new Span<uint>(blockBufferP, blocksPerLong * 8);
            Span<uint> quarterBlockBuffer = new Span<uint>(blockBufferP, blocksPerLong * 2);

            int dataBufferLength = 256;
            ulong* dataBufferP = stackalloc ulong[dataBufferLength];
            Span<ulong> dataBuffer = new Span<ulong>(dataBufferP, dataBufferLength);

            int dataOffset = 0;
            ulong bitBuffer0 = 0;
            ulong bitBuffer1 = 0;
            ulong bitBuffer2 = 0;
            ulong bitBuffer3 = 0;
            ulong bitBuffer4 = 0;
            ulong bitBuffer5 = 0;
            ulong bitBuffer6 = 0;
            ulong bitBuffer7 = 0;

            byte bBitsPerBlock = (byte)bitsPerBlock;
            byte bValueShift = (byte)valueShift;

            Vector128<ulong> vBitsPerBlock = Vector128.Create((ulong)bitsPerBlock);
            Vector128<ulong> vValueShift = Vector128.Create((ulong)valueShift);

            while (blocks.Remaining >= fullBlockBuffer.Length)
            {
                int consumed = blocks.Consume(fullBlockBuffer);
                Debug.Assert(consumed == fullBlockBuffer.Length);

                if (dataOffset + 8 >= dataBuffer.Length)
                {
                    writer.Write(dataBuffer.Slice(0, dataOffset));
                    dataOffset = 0;
                }

                if (writer.Options.UseAvx2Hint && Avx2.IsSupported)
                {
                    Vector256<ulong> vBitBuffer0 = Vector256<ulong>.Zero;
                    Vector256<ulong> vBitBuffer1 = Vector256<ulong>.Zero;

                    for (int j = 0; j < blocksPerLong; j++)
                    {
                        vBitBuffer0 = Avx2.ShiftRightLogical(vBitBuffer0, vBitsPerBlock);
                        vBitBuffer1 = Avx2.ShiftRightLogical(vBitBuffer1, vBitsPerBlock);

                        Vector256<ulong> vBits0 = Vector256.Create(
                            blockBufferP0[j], blockBufferP1[j], blockBufferP2[j], blockBufferP3[j]).AsUInt64();

                        Vector256<ulong> vBits1 = Vector256.Create(
                            blockBufferP4[j], blockBufferP5[j], blockBufferP6[j], blockBufferP7[j]).AsUInt64();

                        vBitBuffer0 = Avx2.Or(vBitBuffer0, Avx2.ShiftLeftLogical(vBits0, vValueShift));
                        vBitBuffer1 = Avx2.Or(vBitBuffer1, Avx2.ShiftLeftLogical(vBits1, vValueShift));
                    }

                    ulong* dataBufferPOff = dataBufferP + dataOffset;
                    Avx.Store(dataBufferPOff + 0, vBitBuffer0);
                    Avx.Store(dataBufferPOff + 4, vBitBuffer1);
                    dataOffset += 8;
                }
                else if (Sse2.IsSupported)
                {
                    Vector128<ulong> vBitBuffer0 = Vector128<ulong>.Zero;
                    Vector128<ulong> vBitBuffer1 = Vector128<ulong>.Zero;
                    Vector128<ulong> vBitBuffer2 = Vector128<ulong>.Zero;
                    Vector128<ulong> vBitBuffer3 = Vector128<ulong>.Zero;

                    for (int j = 0; j < blocksPerLong; j++)
                    {
                        vBitBuffer0 = Sse2.ShiftRightLogical(vBitBuffer0, vBitsPerBlock);
                        vBitBuffer1 = Sse2.ShiftRightLogical(vBitBuffer1, vBitsPerBlock);
                        vBitBuffer2 = Sse2.ShiftRightLogical(vBitBuffer2, vBitsPerBlock);
                        vBitBuffer3 = Sse2.ShiftRightLogical(vBitBuffer3, vBitsPerBlock);

                        Vector128<ulong> vBits0 = Vector128.Create(blockBufferP0[j], blockBufferP1[j]).AsUInt64();
                        Vector128<ulong> vBits1 = Vector128.Create(blockBufferP2[j], blockBufferP3[j]).AsUInt64();
                        Vector128<ulong> vBits2 = Vector128.Create(blockBufferP4[j], blockBufferP5[j]).AsUInt64();
                        Vector128<ulong> vBits3 = Vector128.Create(blockBufferP6[j], blockBufferP7[j]).AsUInt64();

                        vBitBuffer0 = Sse2.Or(vBitBuffer0, Sse2.ShiftLeftLogical(vBits0, vValueShift));
                        vBitBuffer1 = Sse2.Or(vBitBuffer1, Sse2.ShiftLeftLogical(vBits1, vValueShift));
                        vBitBuffer2 = Sse2.Or(vBitBuffer2, Sse2.ShiftLeftLogical(vBits2, vValueShift));
                        vBitBuffer3 = Sse2.Or(vBitBuffer3, Sse2.ShiftLeftLogical(vBits3, vValueShift));
                    }

                    ulong* dataBufferPOff = dataBufferP + dataOffset;
                    Sse2.Store(dataBufferPOff + 0, vBitBuffer0);
                    Sse2.Store(dataBufferPOff + 2, vBitBuffer1);
                    Sse2.Store(dataBufferPOff + 4, vBitBuffer2);
                    Sse2.Store(dataBufferPOff + 6, vBitBuffer3);
                    dataOffset += 8;
                }
                // TODO: ARM intrinsics
                else
                {
                    for (int j = 0; j < blocksPerLong; j++)
                    {
                        bitBuffer0 >>= bitsPerBlock;
                        bitBuffer1 >>= bitsPerBlock;
                        bitBuffer2 >>= bitsPerBlock;
                        bitBuffer3 >>= bitsPerBlock;
                        bitBuffer4 >>= bitsPerBlock;
                        bitBuffer5 >>= bitsPerBlock;
                        bitBuffer6 >>= bitsPerBlock;
                        bitBuffer7 >>= bitsPerBlock;
                        bitBuffer0 |= (ulong)blockBufferP0[j] << valueShift;
                        bitBuffer1 |= (ulong)blockBufferP1[j] << valueShift;
                        bitBuffer2 |= (ulong)blockBufferP2[j] << valueShift;
                        bitBuffer3 |= (ulong)blockBufferP3[j] << valueShift;
                        bitBuffer4 |= (ulong)blockBufferP4[j] << valueShift;
                        bitBuffer5 |= (ulong)blockBufferP5[j] << valueShift;
                        bitBuffer6 |= (ulong)blockBufferP6[j] << valueShift;
                        bitBuffer7 |= (ulong)blockBufferP7[j] << valueShift;
                    }
                    dataBufferP[dataOffset++] = bitBuffer0;
                    dataBufferP[dataOffset++] = bitBuffer1;
                    dataBufferP[dataOffset++] = bitBuffer2;
                    dataBufferP[dataOffset++] = bitBuffer3;
                    dataBufferP[dataOffset++] = bitBuffer4;
                    dataBufferP[dataOffset++] = bitBuffer5;
                    dataBufferP[dataOffset++] = bitBuffer6;
                    dataBufferP[dataOffset++] = bitBuffer7;
                }
            }

            while (blocks.Remaining > 0)
            {
                if (dataOffset == dataBuffer.Length)
                {
                    writer.Write(dataBuffer);
                    dataOffset = 0;
                }

                Span<uint> blockBuffer0 = fullBlockBuffer.Slice(0, blocksPerLong);
                int consumed = blocks.Consume(blockBuffer0);

                ulong bitBuffer = 0;
                for (int j = consumed; j-- > 0;)
                {
                    bitBuffer <<= bitsPerBlock;
                    bitBuffer |= blockBufferP[j];
                }
                dataBufferP[dataOffset++] = bitBuffer;
            }

            writer.Write(dataBuffer.Slice(0, dataOffset));
        }
    }
}
