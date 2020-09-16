using System;
using System.Runtime.Serialization;

namespace MCServerSharp.NBT
{
    public class NbtException : Exception
    {
        public NbtException()
        {
        }

        public NbtException(string message) : base(message)
        {
        }

        public NbtException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NbtException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
