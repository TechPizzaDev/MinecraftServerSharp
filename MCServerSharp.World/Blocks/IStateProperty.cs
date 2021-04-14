using System;

namespace MCServerSharp.Blocks
{
    public interface IStateProperty
    {
        string Name { get; }
        Utf8String NameUtf8 { get; }
        Type ElementType { get; }
        int Count { get; }

        int GetIndex(ReadOnlyMemory<char> value);
        StatePropertyValue GetPropertyValue(int index);
    }
}
