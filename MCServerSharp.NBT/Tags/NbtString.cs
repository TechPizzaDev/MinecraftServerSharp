using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtString : NbTag
    {
        public override NbtType Type => NbtType.String;

        public Utf8String? Value { get; set; }

        public NbtString()
        {
        }

        public NbtString(Utf8String? value)
        {
            Value = value;
        }

        public NbtString(string? value) : this((Utf8String?)value)
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
            if (Value == null)
            {
                writer.Write((ushort)0);
            }
            else
            {
                writer.Write((ushort)Value.Length);
                writer.WriteRaw(Value);
            }
        }
    }
}
