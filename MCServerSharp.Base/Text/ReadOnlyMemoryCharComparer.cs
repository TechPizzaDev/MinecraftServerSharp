using System;
using System.Collections.Generic;

namespace MCServerSharp
{
    public class ReadOnlyMemoryCharComparer : EqualityComparer<ReadOnlyMemory<char>>
    {
        public StringComparison Comparison { get; }

        public ReadOnlyMemoryCharComparer(StringComparison comparison)
        {
            Comparison = comparison;
        }

        public override bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            return x.Span.Equals(y.Span, Comparison);
        }

        public override int GetHashCode(ReadOnlyMemory<char> obj)
        {
            return string.GetHashCode(obj.Span, Comparison);
        }
    }
}
