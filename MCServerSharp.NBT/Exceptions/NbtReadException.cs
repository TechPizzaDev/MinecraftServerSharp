using System;
using System.Runtime.Serialization;

namespace MCServerSharp.NBT
{
    public class NbtReadException : NbtException
    {
        public NbtReadException()
        {
        }

        public NbtReadException(string message) : base(message)
        {
        }

        public NbtReadException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NbtReadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
