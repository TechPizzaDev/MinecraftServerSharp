using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RowFrame
        {
            public static int Size => Unsafe.SizeOf<RowFrame>();

            public const int NumberOfRowsOffset = 0;
            public const int ListTagsLeftOffset = NumberOfRowsOffset + sizeof(int);
            public const int HasNameOffset = ListTagsLeftOffset + sizeof(int);

            public int NumberOfRows;
            public int ListTagsLeft;
            public bool SkipName;

            public RowFrame(int numberOfRows, int listTagsLeft, bool skipName)
            {
                NumberOfRows = numberOfRows;
                ListTagsLeft = listTagsLeft;
                SkipName = skipName;
            }
        }
    }
}
