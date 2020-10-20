using System;

namespace MCServerSharp.Net.Packets
{
    [AttributeUsage(
        AttributeTargets.Method |
        AttributeTargets.Property |
        AttributeTargets.Field,
        AllowMultiple = false,
        Inherited = false)]
    public class DataLengthPrefixedAttribute : Attribute
    {
        public Type LengthType { get; }
        public LengthSource LengthSource { get; }

        public DataLengthPrefixedAttribute(
            Type lengthType, 
            LengthSource lengthSource = LengthSource.ByName)
        {
            if (lengthSource < LengthSource.ByName ||
                lengthSource > LengthSource.WrittenBytes)
                throw new ArgumentOutOfRangeException(nameof(lengthSource));

            LengthType = lengthType ?? throw new ArgumentNullException(nameof(lengthType));
            LengthSource = lengthSource;
        }
    }
}
