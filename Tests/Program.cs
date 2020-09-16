using System;
using MCServerSharp;
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
    }
}
