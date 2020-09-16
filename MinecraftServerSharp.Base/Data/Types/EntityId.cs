using MCServerSharp.Utility;

namespace MCServerSharp
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
