using System;
using MinecraftServerSharp;

namespace Tests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TestVarInt();
            Console.WriteLine(nameof(TestVarInt) + " passed");
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
