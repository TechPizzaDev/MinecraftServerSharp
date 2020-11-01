using System;
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

        [DataProperty(0)] public Chunk Chunk { get; }
        [DataProperty(1)] public bool FullChunk { get; }

        // TODO: create flash-copying (fast deep-clone) of chunks for serialization purposes

        public ServerChunkData(Chunk chunk, bool fullChunk)
        {
            Chunk = chunk;
            FullChunk = fullChunk;
        }

        public void Write(NetBinaryWriter writer)
        {
            writer.Write(Chunk.X);
            writer.Write(Chunk.Z);
            writer.Write(FullChunk);

            int mask = GetSectionMask(Chunk);
            writer.WriteVar(mask);

            var motionBlocking = new NbtLongArray(36);
            writer.Write(motionBlocking.AsCompound((Utf8String)"Heightmaps", (Utf8String)"MOTION_BLOCKING"));

            if (FullChunk)
            {
                Span<int> biomes = stackalloc int[1024];
                biomes.Fill(1); // 1=plains (defined in ServerMain)

                // TODO: optimize by creating WriteVar for Span
                writer.WriteVar(biomes.Length);
                for (int i = 0; i < biomes.Length; i++)
                    writer.WriteVar(biomes[i]);
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

        public static int GetSectionMask(Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            int sectionMask = 0;
            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                if (!chunk[sectionY].IsEmpty)
                    sectionMask |= 1 << sectionY; // Set that bit to true in the mask
            }
            return sectionMask;
        }

        public static int GetChunkSectionDataLength(Chunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            int length = 0;

            for (int sectionY = 0; sectionY < Chunk.SectionCount; sectionY++)
            {
                var section = chunk[sectionY];
                if (!section.IsEmpty)
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
                if (!section.IsEmpty)
                    WriteChunkSection(writer, section);
            }
        }

        private static int GetUnderlyingDataLength(int size, int bitsPerBlock)
        {
            int bytesPerBlock = 64 / bitsPerBlock;
            int longCount = (size + bytesPerBlock - 1) / bytesPerBlock;
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

        // TODO: optimize (vectorize?)
        public static void WriteChunkSection(NetBinaryWriter writer, ChunkSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            IBlockPalette palette = section.BlockPalette;
            int bitsPerBlock = palette.BitsPerBlock;

            writer.Write((short)ChunkSection.BlockCount);

            writer.Write((byte)bitsPerBlock);
            palette.Write(writer);

            int dataLength = GetUnderlyingDataLength(UnderlyingDataSize, bitsPerBlock);

            // A bitmask that contains bitsPerBlock set bits
            uint valueMask = (uint)((1 << bitsPerBlock) - 1);

            Span<ulong> data = dataLength <= 1024 ? stackalloc ulong[dataLength] : new ulong[dataLength];

            ReadOnlySpan<BlockState> blocks = section.Blocks.Span;

            //for (int i = 0; i < blocks.Length; i++)
            //{
            //    int startLong = i * bitsPerBlock / 64;
            //    int startOffset = i * bitsPerBlock % 64;
            //    int endLong = ((i + 1) * bitsPerBlock - 1) / 64;
            //
            //    ulong value = palette.IdForBlock(blocks[i]) & valueMask;
            //
            //    data[startLong] |= value << startOffset;
            //    if (startLong != endLong)
            //        data[endLong] = value >> (64 - startOffset);
            //}


            int dataOffset = 0;
            int spaceInBits = 64;
            ulong bitBuffer = 0;

            for (int i = 0; i < blocks.Length; i++)
            {
                ulong value = palette.IdForBlock(blocks[i]) & valueMask;

                bitBuffer <<= bitsPerBlock;
                bitBuffer |= value;

                spaceInBits -= bitsPerBlock;
                if (spaceInBits < bitsPerBlock)
                {
                    data[dataOffset++] = bitBuffer;
                    bitBuffer = 0;
                    spaceInBits = 64;
                }
            }

            writer.WriteVar(dataLength);
            writer.Write(data);
        }
    }
}
