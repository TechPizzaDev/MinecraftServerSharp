using System;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Collections
{
    internal sealed class LongEnumComparer<TEnum> : LongEqualityComparer<TEnum>
        where TEnum : struct, Enum
    {
        public override long GetLongHashCode([DisallowNull] TEnum value)
        {
            return EnumConverter.ToInt64(value);
        }
    }
}