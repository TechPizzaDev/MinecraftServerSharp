using System;

namespace MCServerSharp.Collections
{
    internal sealed class LongIntPtrComparer : LongEqualityComparer<IntPtr>
    {
        public override long GetLongHashCode(IntPtr obj)
        {
            return obj.ToInt64();
        }
    }

    internal sealed class LongUIntPtrComparer : LongEqualityComparer<UIntPtr>
    {
        public override long GetLongHashCode(UIntPtr obj)
        {
            return unchecked((long)obj.ToUInt64());
        }
    }
}
