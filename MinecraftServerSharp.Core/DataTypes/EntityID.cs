using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp
{
    public readonly struct EntityId
    {
        public int Value { get; }

        public EntityId(int value)
        {
            Value = value;
        }
    }
}
