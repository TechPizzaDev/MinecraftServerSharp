using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        public partial struct MetadataDb
        {
            public ref struct Accessor
            {
                private readonly Span<MetadataDb> _database;

                public Span<byte> RowData { get; private set; }

                public ref MetadataDb Database => ref MemoryMarshal.GetReference(_database);

                internal Accessor(ref MetadataDb database)
                {
                    _database = MemoryMarshal.CreateSpan(ref database, 1);
                    RowData = _database[0]._data.AsSpan();
                }

                public ref DbRow Append(out int index)
                {
                    if (Database.ByteLength >= RowData.Length - DbRow.Size)
                    {
                        Database.Enlarge();
                        RowData = Database._data.AsSpan();
                    }

                    index = Database.ByteLength;
                    Database.ByteLength += DbRow.Size;
                    return ref GetRow(index);
                }

                public ref DbRow GetRow(int index)
                {
                    Database.AssertValidIndex(index);
                    ref byte dataRef = ref MemoryMarshal.GetReference(RowData);
                    return ref Unsafe.As<byte, DbRow>(ref Unsafe.Add(ref dataRef, index));
                }
            }
        }
    }
}
