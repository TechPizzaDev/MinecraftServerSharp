using System;

namespace MCServerSharp.Net.Packets
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class DataLengthConstraintAttribute : Attribute
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Constant { get; set; }

        public DataLengthConstraintAttribute()
        {
        }
    }
}

