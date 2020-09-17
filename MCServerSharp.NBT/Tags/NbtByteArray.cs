using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtByteArray : NbtArray<byte>
    {
        public override NbtType Type => NbtType.ByteArray;
        
        public NbtByteArray(Utf8String? name, int count) : base(name, count)
        {
        }

        public NbtByteArray(int count) : base(null, count)
        {
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Items);
        }
    }
}
