using System;
using MCServerSharp.Text;

namespace MCServerSharp
{
    public interface IIdentifier<T> : IEquatable<T>
        where T : IIdentifier<T>
    {
        RuneEnumerator EnumerateValue();
        RuneEnumerator EnumerateNamespace();
        RuneEnumerator EnumerateLocation();
    }
}
