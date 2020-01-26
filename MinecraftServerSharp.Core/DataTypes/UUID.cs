
namespace MinecraftServerSharp
{
    public readonly struct UUID
    {
        public ulong X { get; }
        public ulong Y { get; }

        public UUID(ulong x, ulong y)
        {
            X = x;
            Y = y;
        }
    }
}
