using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using MCServerSharp;
using MCServerSharp.Data.IO;
using MCServerSharp.IO.Compression;
using MCServerSharp.NBT;
using MCServerSharp.Utility;

namespace Tests
{
    internal class Tests
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

            // Find the first valid chunk location.
            int firstValidLocation = 0;
            for (int i = 0; i < locations.Length; i++)
            {
                if (locations[i].SectorCount != 0)
                {
                    firstValidLocation = i;
                    break;
                }
            }
            int chunkCount = 1024 - firstValidLocation;

            var chunkList = new (int Index, NbtDocument Chunk)[chunkCount];

            var compressedData = new MemoryStream();
            var decompressedData = new MemoryStream();

            for (int i = 0; i < chunkList.Length; i++)
            {
                int locationIndex = firstValidLocation + i;
                var location = locations[locationIndex];
                int chunkIndex = locationIndices[locationIndex];

                var lengthStatus = reader.Read(out int actualDataLength);
                if (lengthStatus != OperationStatus.Done)
                    continue;

                var compressionTypeStatus = reader.Read(out byte compressionType);
                if (compressionTypeStatus != OperationStatus.Done)
                    continue;

                int remainingByteCount = location.SectorCount * 4096 - sizeof(int) - sizeof(byte);

                compressedData.Position = 0;
                var compressedDataStatus = reader.WriteTo(compressedData, remainingByteCount);
                if (compressedDataStatus != OperationStatus.Done)
                    continue;

                compressedData.SetLength(actualDataLength - 1);
                compressedData.Position = 0;

                Stream dataStream = compressionType switch
                {
                    1 => new GZipStream(compressedData, CompressionMode.Decompress, leaveOpen: true),
                    2 => new ZlibStream(compressedData, CompressionMode.Decompress, leaveOpen: true),
                    3 => compressedData,
                    _ => throw new InvalidDataException("Unknown compression type.")
                };

                decompressedData.SetLength(0);
                decompressedData.Position = 0;
                dataStream.SCopyTo(decompressedData);

                var chunkData = decompressedData.GetBuffer().AsMemory(0, (int)decompressedData.Length);
                var chunkDocument = NbtDocument.Parse(chunkData, out int bytesConsumed, NbtOptions.JavaDefault);
                chunkList[i] = (i, chunkDocument);


                var root = chunkDocument.RootTag;
                //PrintContainer(root);

                void PrintContainer(NbtElement container)
                {
                    foreach (var element in container.EnumerateContainer())
                    {
                        Console.WriteLine(element);
                        if (element.Type.IsContainer())
                        {
                            PrintContainer(element);
                        }
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
