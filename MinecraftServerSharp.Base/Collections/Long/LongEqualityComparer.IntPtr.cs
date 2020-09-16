using System;

namespace MCServerSharp.Collections
{
    public partial class LongEqualityComparer<T>
    {
        private class LongIntPtrComparer : LongEqualityComparer<IntPtr>
        {
            public override long GetLongHashCode(IntPtr obj) => obj.ToInt64();
        }

        private class LongUIntPtrComparer : LongEqualityComparer<UIntPtr>
        {
            public override long GetLongHashCode(UIntPtr obj) => unchecked((long)obj.ToUInt64());
        }
    }
}
