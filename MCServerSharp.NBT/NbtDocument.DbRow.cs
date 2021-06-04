using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        /// <summary>
        /// One <see cref="DbRow"/> corresponds to one tag in a <see cref="MetadataDb"/>,
        /// excluding <see cref="NbtType.End"/> tags.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DbRow
        {
            public static int Size => Unsafe.SizeOf<DbRow>();

            public readonly int Location;

            /// <summary>
            /// The amount of elements in a container tag.
            /// </summary>
            public int CollectionLength;

            public int RowCount;

            public readonly int RawNameLength;

            public readonly NbtFlags Flags;
            public readonly NbtType Type;

            public bool IsContainerType => Type.IsContainer();
            public bool IsPrimitiveType => Type.IsPrimitive();

            public DbRow(
                int location, int collectionLength, int rowCount, int rawNameLength,
                NbtType type, NbtFlags flags)
            {
                Debug.Assert(location >= 0);
                Debug.Assert(type >= NbtType.End && type <= NbtType.LongArray);

                Location = location;
                CollectionLength = collectionLength;
                RowCount = rowCount;
                RawNameLength = rawNameLength;
                Type = type;
                Flags = flags;
            }
        }
    }
}
