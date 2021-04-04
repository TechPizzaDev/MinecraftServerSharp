using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtString : NbTag
    {
        public static NbtString Null { get; } = new();
        public static NbtString Empty { get; } = new(Utf8String.Empty);

        protected Utf8String? _value;

        public Utf8String? Value { get => _value; init => _value = value; }

        public override NbtType Type => NbtType.String;

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
            Utf8String? value = _value;
            if (value == null || value.Length == 0)
            {
                writer.Write((ushort)0);
            }
            else
            {
                writer.Write((ushort)value.Length);
                writer.WriteRaw(value);
            }
        }
    }

    public class NbtMutString : NbtString
    {
        public new Utf8String? Value { get => _value; set => _value = value; }

        public NbtMutString()
        {
        }

        public NbtMutString(Utf8String? value)
        {
            Value = value;
        }
    }
}
