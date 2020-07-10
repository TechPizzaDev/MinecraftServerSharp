
namespace MinecraftServerSharp.NBT
{
    public abstract class NbtContainer<TSelf> : NbTag
        where TSelf : class
    {
        public abstract int Count { get; }

        protected NbtContainer(Utf8String? name) : base(name)
        {
        }

        public abstract TSelf Add(NbTag tag);
    }
}
