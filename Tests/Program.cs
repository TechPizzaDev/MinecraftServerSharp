using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MCServerSharp;
using MCServerSharp.Data.IO;
using MCServerSharp.IO.Compression;
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

            var reader = new NetBinaryReader(stream, NetBinaryOptions.JavaDefault);

            var locations = new ChunkLocation[1024];
            var locationsStatus = reader.Read(MemoryMarshal.AsBytes(locations.AsSpan()));

            var locationIndices = new int[1024];
            for (int i = 0; i < locationIndices.Length; i++)
                locationIndices[i] = i;

            Array.Sort(locations, locationIndices);

            var timestamps = new int[1024];
            var timestampsStatus = reader.Read(timestamps);

            //var document = NbtDocument.Parse(buffer.AsMemory(0, totalRead), out int consumed);
            //Console.WriteLine(document.RootTag);

            int start = 0;
            for (int i = 0; i < locations.Length; i++)
            {
                if (locations[i].SectorCount != 0)
                {
                    start = i;
                    break;
                }
            }
            int count = 1024 - start;

            var chunkList = new (int Index, NbtDocument)[count];

            for (int i = 0; i < chunkList.Length; i++)
            {
                int locationIndex = start + i;
                var location = locations[locationIndex];
                int chunkIndex = locationIndices[locationIndex];

                int sectorCount = location.SectorCount;
                int byteCount = sectorCount * 4096;

                var lengthStatus = reader.Read(out int length);
                var compressionTypeStatus = reader.Read(out byte compressionType);

                var compressedData = reader.ReadBytes(byteCount);
                var compressedStream = new MemoryStream(compressedData, 0, length - 1);

                Stream dataStream = compressionType switch
                {
                    1 => new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true),
                    2 => new ZlibStream(compressedStream, CompressionMode.Decompress, leaveOpen: true),
                    3 => compressedStream,
                    _ => throw new InvalidDataException("Unknown compression type.")
                };

                var decompressedData = new MemoryStream();
                dataStream.CopyTo(decompressedData);
                decompressedData.Position = 0;

                var chunkData = decompressedData.GetBuffer().AsMemory(0, (int)decompressedData.Length);
                var chunkDocument = NbtDocument.Parse(chunkData, out int bytesConsumed, NbtOptions.JavaDefault);
                Console.WriteLine(chunkDocument);

                for (int r = 0; r < chunkDocument._metaDb.ByteLength; r += NbtDocument.DbRow.Size)
                {
                    ref readonly var row = ref chunkDocument._metaDb.GetRow(r);

                    Console.WriteLine(row.TagType + " : c" + row.NumberOfRows);
                }

                var root = chunkDocument.RootTag;
                PrintContainer(root);

                void PrintContainer(NbtElement container)
                {
                    foreach (var element in root.EnumerateContainer())
                    {
                        Console.WriteLine(element.Type + ": " );
                        if (element.Type.IsContainer())
                        {
                            PrintContainer(element);
                        }
                        Console.WriteLine();
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        public readonly struct ChunkLocation : IEquatable<ChunkLocation>, IComparable<ChunkLocation>
        {
            private readonly byte _offset0;
            private readonly byte _offset1;
            private readonly byte _offset2;

            public byte SectorCount { get; }

            public int SectorOffset => _offset0 << 16 | _offset1 << 8 | _offset2;

            public int CompareTo(ChunkLocation other)
            {
                return SectorOffset.CompareTo(other.SectorOffset);
            }

            public bool Equals(ChunkLocation other)
            {
                return SectorCount == other.SectorCount
                    && _offset0 == other._offset0
                    && _offset1 == other._offset1
                    && _offset2 == other._offset2;
            }

            private string GetDebuggerDisplay()
            {
                return ToString();
            }

            public override int GetHashCode()
            {
                return UnsafeR.As<ChunkLocation, int>(this);
            }

            public override string ToString()
            {
                return $"{SectorCount} @ [{SectorOffset}]";
            }
        }
    }
}
