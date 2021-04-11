using System;

namespace MCServerSharp.Blocks
{
    public interface IStateProperty
    {
        string Name { get; }
        Type ElementType { get; }
        int Count { get; }

        int GetIndex(ReadOnlyMemory<char> value);
        StatePropertyValue GetPropertyValue(int index);
    }
}
