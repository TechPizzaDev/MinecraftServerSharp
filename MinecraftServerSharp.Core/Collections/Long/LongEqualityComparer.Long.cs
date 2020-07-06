
namespace MinecraftServerSharp.Collections
{
    public partial class LongEqualityComparer<T>
    {
        private class LongInt64Comparer : LongEqualityComparer<long>
        {
            public override long GetLongHashCode(long obj) => obj;
        }

        private class LongUInt64Comparer : LongEqualityComparer<ulong>
        {
            public override long GetLongHashCode(ulong obj) => unchecked((long)obj);
        }
    }
}
