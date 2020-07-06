using System;

namespace MinecraftServerSharp
{
    /// <summary>
    /// The exception that is thrown when trying to use an empty instance.
    /// <para>
    /// Commonly thrown when checking whether a struct
    /// (e.g <see cref="Span{T}"/> or <see cref="Memory{T}"/>) or collection is empty.
    /// </para>
    /// </summary>
    public class ArgumentEmptyException : ArgumentException
    {
        public ArgumentEmptyException()
        {
        }

        public ArgumentEmptyException(string paramName) : base(string.Empty, paramName)
        {
        }

        public ArgumentEmptyException(string message, Exception inner) : base(message, inner)
        {
        }

        public ArgumentEmptyException(string paramName, string message) : base(message, paramName)
        {
        }

        public ArgumentEmptyException(string paramName, string message, Exception inner) : 
            base(message, paramName, inner)
        {
        }
    }
}
