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
            public int CollectionLength { get; }

            public int RowCount { get; }

            public NbtFlags Flags { get; }
            public NbtType Type { get; }

            public bool IsContainerType => Type.IsContainer();
            public bool IsPrimitiveType => Type.IsPrimitive();

            public DbRow(int location, int collectionLength, int rowCount, NbtType type, NbtFlags flags)
            {
                Debug.Assert(location >= 0);
                Debug.Assert(type >= NbtType.End && type <= NbtType.LongArray);

                Location = location;
                CollectionLength = collectionLength;
                RowCount = rowCount;
                Type = type;
                Flags = flags;
            }
        }
    }
}
