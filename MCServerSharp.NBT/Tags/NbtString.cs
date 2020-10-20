using System;
using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtString : NbTag
    {
        public override NbtType Type => NbtType.String;

        public Utf8String Value { get; }

        public NbtString(Utf8String? name, Utf8String value) : base(name)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public NbtString(Utf8String? name, string value) : this(name, (Utf8String)value)
        {
        }

        public override void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            base.Write(writer, flags);

            writer.Write((ushort)Value.Length);
            writer.WriteRaw(Value);
        }
    }
}
