
namespace MinecraftServerSharp.NBT
{
    public class NbtEnd : NbTag
    {
        public static NbtEnd Instance { get; } = new NbtEnd();

        public override NbtType Type => NbtType.End;

        public NbtEnd() : base(null)
        {
        }
    }
}
