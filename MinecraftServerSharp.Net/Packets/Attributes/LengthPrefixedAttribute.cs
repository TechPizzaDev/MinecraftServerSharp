using System;

namespace MCServerSharp.Net.Packets
{
    [AttributeUsage(
        AttributeTargets.Method |
        AttributeTargets.Property |
        AttributeTargets.Field,
        AllowMultiple = false,
        Inherited = false)]
    public class LengthPrefixedAttribute : Attribute
    {
        public Type LengthType { get; }
        public LengthSource LengthSource { get; }

        public LengthPrefixedAttribute(
            Type lengthType, 
            LengthSource lengthSource = LengthSource.CollectionLength)
        {
            if (lengthSource < LengthSource.CollectionLength ||
                lengthSource > LengthSource.WrittenBytes)
                throw new ArgumentOutOfRangeException(nameof(lengthSource));

            LengthType = lengthType ?? throw new ArgumentNullException(nameof(lengthType));
            LengthSource = lengthSource;
        }
    }
}
