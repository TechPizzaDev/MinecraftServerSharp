using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtLongArray : NbtArray
    {
        private long[] _array;

        public override NbtType Type => NbtType.LongArray;

        public NbtLongArray(int length, Utf8String? name = null) : base(length, name)
        {
            _array = new long[length];
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(_array);
        }
    }
}
