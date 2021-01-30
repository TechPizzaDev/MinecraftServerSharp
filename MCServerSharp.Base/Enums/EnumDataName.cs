using System;

namespace MCServerSharp
{
    // TOOD: implement

    public enum EnumDataNameSource
    {
        DataNameOrdinalIgnoreCase,
        DataNameOrdinal,
        EnumOrdinalIgnoreCase,
        EnumOrdinal,
    }

    [AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class EnumDataNameAttribute : Attribute
    {
        public string? DataName { get; }
        public EnumDataNameSource NameSource { get; }

        public EnumDataNameAttribute(string? dataName)
        {
            DataName = dataName;
            NameSource = EnumDataNameSource.DataNameOrdinalIgnoreCase;
        }

        public EnumDataNameAttribute(EnumDataNameSource nameSource)
        {
            NameSource = nameSource;
        }
    }
}
