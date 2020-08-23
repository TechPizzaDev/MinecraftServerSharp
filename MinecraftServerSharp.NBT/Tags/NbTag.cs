using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.NBT
{
    public abstract class NbTag
    {
        public abstract NbtType Type { get; }
        public Utf8String? Name { get; }

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
