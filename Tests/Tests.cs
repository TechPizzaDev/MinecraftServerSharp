using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
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

            var files = Directory.GetFiles($@"..\..\..\..\MCJarServer\1.15.2\world\region");
            foreach (string file in files)
            {
                //using var stream = File.OpenRead(
                //   $@"..\..\..\..\MCJarServer\1.15.2\world\region\r.{chunkX}.{chunkZ}.mca");

                using var stream = File.OpenRead(file);
                
                int regionX = chunkX / 32;
                int regionZ = chunkZ / 32;

                var reader = new NetBinaryReader(stream, NetBinaryOptions.JavaDefault);

                var locations = new ChunkLocation[1024];
                var locationsStatus = reader.Read(MemoryMarshal.AsBytes(locations.AsSpan()));

                var timestamps = new int[1024];
                var timestampsStatus = reader.Read(timestamps);

                var locationIndices = new int[1024];
                for (int i = 0; i < locationIndices.Length; i++)
                    locationIndices[i] = i;

                Array.Sort(locations, locationIndices);

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

                    // Some files have empty sectors between chunks so check if skip is needed.
                    long sectorPosition = location.SectorOffset * 4096;
                    if (reader.Position != sectorPosition) // this should always be a multiple of 4096
                    {
                        long toSkip = sectorPosition - reader.Position;
                        if (toSkip < 0) // locations are ordered by offset so this should never throw
                            throw new InvalidDataException();
                        reader.Seek(toSkip, SeekOrigin.Current);
                    }

                    var lengthStatus = reader.Read(out int actualDataLength);
                    if (lengthStatus != OperationStatus.Done)
                        throw new EndOfStreamException();

                    var compressionTypeStatus = reader.Read(out byte compressionType);
                    if (compressionTypeStatus != OperationStatus.Done)
                        throw new EndOfStreamException();

                    int remainingByteCount = location.SectorCount * 4096 - sizeof(int) - sizeof(byte);

                    compressedData.Position = 0;
                    var compressedDataStatus = reader.WriteTo(compressedData, remainingByteCount);
                    if (compressedDataStatus != OperationStatus.Done)
                        throw new EndOfStreamException();

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
                    dataStream.SpanCopyTo(decompressedData);

                    var chunkData = decompressedData.GetBuffer().AsMemory(0, (int)decompressedData.Length);

                    int chunkIndex = locationIndices[locationIndex];
                    var chunkDocument = NbtDocument.Parse(chunkData, out int bytesConsumed, NbtOptions.JavaDefault);
                    chunkList[i] = (chunkIndex, chunkDocument);

                    var rootCompound = chunkDocument.RootTag;
                    var rootClone = rootCompound.Clone();

                    // TODO: add some kind of NbtDocument-to-(generic)object helper and NbtSerializer

                    var levelCompound = rootCompound["Level"];
                    var sectionsList = levelCompound["Sections"];

                    foreach (var sectionCompound in sectionsList.EnumerateContainer())
                    {
                        var yInt = sectionCompound["Y"].GetInt();
                        if (yInt == -1)
                            continue;

                        var paletteList = sectionCompound["Palette"];
                        var blockList = sectionCompound["BlockStates"];
                        var blockData = blockList.GetArrayData(out var tagType);
                        if (tagType != NbtType.LongArray)
                            throw new InvalidDataException();


                    }

                    static void CompareContainers(NbtElement container1, NbtElement container2)
                    {
                        var enum1 = container1.EnumerateContainer();
                        var enum2 = container2.EnumerateContainer();
                        bool move;
                        do
                        {
                            move = enum1.MoveNext();
                            if (move != enum2.MoveNext())
                                throw new Exception("Not equal move");

                            if (move)
                            {
                                var span1 = enum1.Current.GetRawData().Span;
                                var span2 = enum2.Current.GetRawData().Span;
                                if (!span1.SequenceEqual(span2))
                                    throw new Exception("Not equal element");
                            }
                        }
                        while (move);
                    }

                    CompareContainers(rootCompound, rootClone);

                    //PrintContainer(root, 1);
                    //
                    //void PrintContainer(NbtElement container, int depth)
                    //{
                    //    string space = new string(' ', depth * 2);
                    //    foreach (var element in container.EnumerateContainer())
                    //    {
                    //        //Console.WriteLine(space + element);
                    //
                    //        switch (element.Type)
                    //        {
                    //            case NbtType.Compound:
                    //            case NbtType.List:
                    //                PrintContainer(element, depth + 1);
                    //                break;
                    //        }
                    //    }
                    //}
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
