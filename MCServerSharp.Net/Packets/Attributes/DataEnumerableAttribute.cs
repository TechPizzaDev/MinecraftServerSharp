using System;

namespace MCServerSharp.Net.Packets
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class DataEnumerableAttribute : Attribute
    {
    }
}
