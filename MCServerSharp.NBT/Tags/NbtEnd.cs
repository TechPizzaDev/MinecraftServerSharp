using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtEnd : NbTag
    {
        public static NbtEnd Instance { get; } = new();

        public override NbtType Type => NbtType.End;

        public NbtEnd()
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
        }

        public static void Write(NetBinaryWriter writer, NbtFlags flags)
        {
            Instance.WriteHeader(writer, flags);
        }
    }
}
