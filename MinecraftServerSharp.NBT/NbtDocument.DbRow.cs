using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct DbRow
        {
            public static int Size => Unsafe.SizeOf<DbRow>();

            public const int LocationOffset = 0;
            public const int LengthOffset = LocationOffset + sizeof(int);
            public const int NumberOfRowsOffset = LengthOffset + sizeof(int);
            public const int TagTypeOffset = NumberOfRowsOffset + sizeof(int);

            public int Location { get; }

            /// <summary>
            /// The amount of elements in an array-like tag or
            /// the size of a primitive tag in bytes.
            /// </summary>
            public int Length { get; }

            public int NumberOfRows { get; }

            public NbtType TagType { get; }
            public bool HasName { get; }

            public bool IsContainerType => TagType.IsContainer();
            public bool IsPrimitiveType => TagType.IsPrimitive();

            public DbRow(int location, int length, int numberOfRows, NbtType tagType, bool hasName)
            {
                Debug.Assert(location >= 0);
                Debug.Assert(numberOfRows >= 0);
                Debug.Assert(tagType >= NbtType.End && tagType <= NbtType.LongArray);

                Location = location;
                Length = length;
                NumberOfRows = numberOfRows;
                TagType = tagType;
                HasName = hasName;
            }
        }
    }
}
