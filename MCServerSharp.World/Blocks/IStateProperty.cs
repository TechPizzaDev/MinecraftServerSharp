using System;

namespace MCServerSharp.Blocks
{
    public interface IStateProperty
    {
        string NameUtf16 { get; }
        Utf8String Name { get; }
        Type ElementType { get; }
        int Count { get; }

        int GetIndex(ReadOnlyMemory<char> value);
        int GetIndex(Utf8Memory value);
    }
}
