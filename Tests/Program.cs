using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MCServerSharp;
using MCServerSharp.Data.IO;
using MCServerSharp.NBT;
using MCServerSharp.Utility;

namespace Tests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TestVarInt();
            Console.WriteLine(nameof(TestVarInt) + " passed");

            TestStreamTrimStart();
            Console.WriteLine(nameof(TestStreamTrimStart) + " passed");

            TestNbtRegionFileRead();
            Console.WriteLine(nameof(TestNbtRegionFileRead) + " passed");
        }

        private static void TestStreamTrimStart()
        {
            var mem = new RecyclableMemoryManager(2, 2, 2);
            var stream = mem.GetStream(6);
            for (int i = 0; i < 6; i++)
                stream.WriteByte((byte)(i % 255));

            stream.TrimStart(3);
            if (stream.GetBlock(0).Span[0] != 3 ||
                stream.GetBlock(0).Span[1] != 4 ||
                stream.GetBlock(1).Span[0] != 5)
                throw new Exception();
        }

        #region TestVarInt

        private static void TestVarInt()
        {
            TestVarInt(0, 0);
            TestVarInt(1, 1);
            TestVarInt(2, 2);
            TestVarInt(127, 127);
            TestVarInt(128, 128, 1);
            TestVarInt(255, 255, 1);
            TestVarInt(2147483647, 255, 255, 255, 255, 7);
            TestVarInt(-1, 255, 255, 255, 255, 15);
            TestVarInt(-2147483648, 128, 128, 128, 128, 8);
        }

        private static void TestVarInt(int decimalValue, params byte[] bytes)
        {
            Span<byte> tmp = stackalloc byte[VarInt.MaxEncodedSize];
            int len = new VarInt(decimalValue).Encode(tmp);
            if (!tmp.Slice(0, len).SequenceEqual(bytes))
                throw new Exception();
        }

        #endregion

        private static void TestNbtRegionFileRead()
        {
            // code adapted from https://github.com/rantingmong/blocm/blob/master/blocm_core/RegionFile.cs

            int chunkX = 0;
            int chunkZ = 0;

            using var stream = File.OpenRead(
               $@"..\..\..\..\MCJarServer\1.15.2\world\region\r.{chunkX}.{chunkZ}.mca");

            int regionX = chunkX / 32;
            int regionZ = chunkZ / 32;

            var reader = new NetBinaryReader(stream);

            var sectors = new ChunkLocation[1024];
            var sectorsStatus = reader.Read(MemoryMarshal.Cast<ChunkLocation, int>(sectors));

            var timestamps = new int[1024];
            var timestampsStatus = reader.Read(timestamps);

            //var document = NbtDocument.Parse(buffer.AsMemory(0, totalRead), out int consumed);
            //Console.WriteLine(document.RootTag);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        public readonly struct ChunkLocation : IEquatable<ChunkLocation>
        {
            public byte SectorCount { get; }

            private readonly byte _offset0;
            private readonly byte _offset1;
            private readonly byte _offset2;

            public int Offset => _offset0 | _offset1 >> 8 | _offset2 >> 16;

            private string GetDebuggerDisplay()
            {
                return ToString();
            }

            public bool Equals(ChunkLocation other)
            {
                return SectorCount == other.SectorCount
                    && _offset0 == other._offset0
                    && _offset1 == other._offset1
                    && _offset2 == other._offset2;
            }

            public override int GetHashCode()
            {
                return UnsafeR.As<ChunkLocation, int>(this);
            }

            public override string ToString()
            {
                return $"{SectorCount} @ [{Offset}]";
            }
        }
    }
}
