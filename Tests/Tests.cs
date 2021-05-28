using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using MCServerSharp;
using MCServerSharp.AnvilStorage;
using MCServerSharp.Data.IO;
using MCServerSharp.IO.Compression;
using MCServerSharp.NBT;
using MCServerSharp.Utility;
using MCServerSharp.World;

namespace Tests
{
    internal class Tests
    {
        private static void Main(string[] args)
        {
            TestBitArray32();
            Console.WriteLine(nameof(TestBitArray32) + " passed");

            TestVarInt();
            Console.WriteLine(nameof(TestVarInt) + " passed");

            TestUtf8String();
            Console.WriteLine(nameof(TestUtf8String) + " passed");

            TestStreamTrimStart();
            Console.WriteLine(nameof(TestStreamTrimStart) + " passed");

            TestNbtRegionFileRead();
            Console.WriteLine(nameof(TestNbtRegionFileRead) + " passed");
        }

        private static void TestBitArray32()
        {
            Span<uint> tmp = new uint[4096];

            var array = BitArray32.Allocate(4096, 4);
            array.Set(8, 3);
            array.Set(9, 3);
            array.Set(10, 3);
            uint e = array.Get(9);

            array.Set(32 + 6, 3);
            array.Set(510, 2);

            ulong sum = 0;
            sum = (uint)array.Get(0, tmp.Slice(0, 511));

            for (int i = 0; i < 1024; i++)
            {
                sum += (uint)array.Get(0, tmp);
            }

            Console.WriteLine(array + " " + sum);
        }

        private static void TestUtf8String()
        {
            Rune rune = Rune.GetRuneAt("😃", 0);

            byte[] t = new byte[12];
            t[0] = (byte)'a';
            t[1] = (byte)'b';
            t[2] = (byte)'c';
            t[3] = (byte)'d';
            rune.EncodeToUtf8(t.AsSpan(4));
            t[8 + 0] = (byte)'e';
            t[8 + 1] = (byte)'f';
            t[8 + 2] = (byte)'g';
            t[8 + 3] = (byte)'h';

            var s = new Utf8Splitter(t, default, StringSplitOptions.None);
            foreach (Range range in s)
            {
                var ssss = new Utf8String(s.Span[range]);
                Console.WriteLine(range + ": " + ssss);
            }

            for (int i = 0; i < t.Length; i++)
            {
                Console.Write("[" + i + "] + " + (t.Length - i) + ": ");
                Console.Write(Utf8String.IsValidUtf8Slice(t, i, t.Length - i));
                Console.Write(", ");
                Console.WriteLine(Utf8String.IsValidUtf8Slice(t, i, 1));
            }

            Utf8String utf8 = " x  this  is cool    ".ToUtf8String();

            Thread.Sleep(1000);

            var rng = new Random();

            utf8 = Utf8String.Create(1024 * 1024 * 4, 0, (span, u) =>
            {
                rng.NextBytes(span);
            });

            Console.WriteLine("split: ");
            //var split = utf8.EnumerateRangeSplit(" ".ToUtf8String());
            //foreach (Range range in split)
            //{
            //    Utf8String sub = utf8.Substring(range);
            //    //Console.WriteLine($"\"{sub}\"");
            //}

            Console.WriteLine("remove empty split: ");
            var removesplit = utf8.EnumerateSplit(" ".ToUtf8String(), StringSplitOptions.None);
            int count = 0;
            int invalidCount = 0;
            while (removesplit.MoveNext())
            {
                (int offset, int length) = removesplit.Current.GetOffsetAndLength(removesplit.Span.Length);
                if (!Utf8String.IsValidUtf8Slice(removesplit.Span, offset, length))
                    invalidCount++;

                //Utf8String sub = utf8.Substring(range);
                //Console.WriteLine($"\"{sub}\"");
                count++;
            }
            Console.WriteLine("Utf8String: " + count);
            Console.WriteLine("Utf8String Invalids: " + invalidCount);

            //Console.WriteLine("String: " + utf8.ToString().Split(" ", StringSplitOptions.None).Length);

            Console.WriteLine();
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

            string[] files;
            if (Directory.Exists("region"))
            {
                files = Directory.GetFiles("region");
            }
            else
            {
                files = Directory.GetFiles($@"..\..\..\..\MCJarServer\1.16.5\world\region");
            }

            var compressedData = new MemoryStream();
            var decompressedData = new MemoryStream();

            foreach (string file in files)
            {
                //using var stream = File.OpenRead(
                //   $@"..\..\..\..\MCJarServer\1.15.2\world\region\r.{chunkX}.{chunkZ}.mca");

                using var stream = File.OpenRead(file);

                compressedData.Position = 0;
                compressedData.SetLength(0);
                decompressedData.Position = 0;
                decompressedData.SetLength(0);

                //int regionX = chunkX / 32;
                //int regionZ = chunkZ / 32;

                var reader = new NetBinaryReader(stream, NetBinaryOptions.JavaDefault);

                var regionReaderStatus = AnvilRegionReader.Create(reader, ArrayPool<byte>.Shared, out var regionReader);

                Stopwatch watch = Stopwatch.StartNew();
                regionReader.CompleteFullLoad(default).AsTask().Wait();
                watch.Stop();
                Console.WriteLine($"Loaded {regionReader.ChunkCount} chunks for region " + " in " + watch.ElapsedMilliseconds + "ms");

                string dirname = "chunksnbt";
                Directory.CreateDirectory(dirname);
                using var filwriter = new FileStream($"{dirname}/{Path.GetFileName(file)}.chunksnbt", FileMode.Create);
                using var binwriter = new BinaryWriter(filwriter);

                binwriter.Write(regionReader.ChunkCount);

                for (int i = 0; i < regionReader.ChunkCount; i++)
                {
                    int locationIndex = regionReader.FirstValidLocation + i;
                    ChunkLocation location = regionReader.Locations[locationIndex];

                    AnvilChunkDocument? anvilDocument = regionReader.LoadAsync(i, default).AsTask().Result;
                    NbtDocument? chunkDocument = anvilDocument.GetValueOrDefault().Document;

                    Debug.Assert(chunkDocument != null);

                    NbtElement rootCompound = chunkDocument.RootTag;
                   
                    //continue;
                    //NbtElement rootClone = rootCompound.Clone();

                    binwriter.Write(chunkDocument.Bytes.Length);
                    binwriter.Write(chunkDocument.Bytes.Span);

                    // TODO: add some kind of NbtDocument-to-(generic)object helper and NbtSerializer

                    //NbtElement levelCompound = rootCompound["Level"];
                    //NbtElement sectionsList = levelCompound["Sections"];
                    //
                    //foreach (NbtElement sectionCompound in sectionsList.EnumerateContainer())
                    //{
                    //    int yInt = sectionCompound["Y"].GetInt();
                    //    if (yInt == -1)
                    //        continue;
                    //
                    //    NbtElement paletteList = sectionCompound["Palette"];
                    //    NbtElement blockList = sectionCompound["BlockStates"];
                    //    ReadOnlyMemory<byte> blockData = blockList.GetArrayData(out NbtType tagType);
                    //    if (tagType != NbtType.LongArray)
                    //        throw new InvalidDataException();
                    //
                    //
                    //}

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
                                var span1 = enum1.Current.GetRawDataSpan(out _);
                                var span2 = enum2.Current.GetRawDataSpan(out _);
                                if (!span1.SequenceEqual(span2))
                                    throw new Exception("Not equal element");
                            }
                        }
                        while (move);
                    }

                    //CompareContainers(rootCompound, rootClone);

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

                Console.WriteLine("Parsed " + regionReader.ChunkCount + " chunks in " + file);
            }
        }
    }
}
