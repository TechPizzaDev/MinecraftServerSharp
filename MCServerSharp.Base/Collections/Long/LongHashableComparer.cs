using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Collections
{
    internal sealed class LongHashableComparer<T> : LongEqualityComparer<T>
        where T : ILongHashable
    {
        public override long GetLongHashCode([DisallowNull] T value)
        {
            return value?.GetLongHashCode() ?? 0;
        }
    }
}