using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct DbRow
        {
            public static int Size => Unsafe.SizeOf<DbRow>();

            public const int LocationOffset = 0;
            public const int LengthOffset = LocationOffset + sizeof(int);
            public const int NumberOfRowsOffset = LengthOffset + sizeof(int);
            public const int FlagsOffset = NumberOfRowsOffset + sizeof(int);
            public const int TagTypeOffset = FlagsOffset + sizeof(NbtFlags);

            public int Location { get; }

            /// <summary>
            /// The amount of elements in a container tag.
            /// </summary>
            public int ContainerLength { get; }

            public int NumberOfRows { get; }

            public NbtFlags Flags { get; }
            public NbtType TagType { get; }

            public bool IsContainerType => TagType.IsContainer();
            public bool IsPrimitiveType => TagType.IsPrimitive();

            public DbRow(int location, int containerLength, int numberOfRows, NbtType tagType, NbtFlags flags)
            {
                Debug.Assert(location >= 0);
                Debug.Assert(tagType >= NbtType.End && tagType <= NbtType.LongArray);

                Location = location;
                ContainerLength = containerLength;
                NumberOfRows = numberOfRows;
                TagType = tagType;
                Flags = flags;
            }
        }
    }
}
