using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public abstract class NbTag
    {
        public Utf8String? Name { get; }

        public abstract NbtType Type { get; }

        // TODO: system.string constructor

        public NbTag(Utf8String? name)
        {
            Name = name;
        }

        public virtual void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            if (flags.HasFlag(NbtFlags.Typed))
                writer.Write((byte)Type);

            if (flags.HasFlag(NbtFlags.Named) && Name != null)
            {
                writer.Write((ushort)Name.Length);
                writer.WriteRaw(Name);
            }
        }
    }
}
