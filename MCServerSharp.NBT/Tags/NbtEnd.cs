using MCServerSharp.Data.IO;

namespace MCServerSharp.NBT
{
    public class NbtEnd : NbTag
    {
        public override NbtType Type => NbtType.End;

        public NbtEnd()
        {
        }

        public override void WritePayload(NetBinaryWriter writer, NbtFlags flags)
        {
        }
    }
}
