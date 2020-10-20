using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtLong : NbTag
    {
        public override NbtType Type => NbtType.Long;

        public long Value { get; }

        public NbtLong(Utf8String? name, long value) : base(name)
        {
            Value = value;
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write(Value);
        }
    }
}
