
namespace MCServerSharp.Collections
{
    internal sealed class LongInt64Comparer : LongEqualityComparer<long>
    {
        public override long GetLongHashCode(long obj) => obj;
    }

    internal sealed class LongUInt64Comparer : LongEqualityComparer<ulong>
    {
        public override long GetLongHashCode(ulong obj) => unchecked((long)obj);
    }
}
