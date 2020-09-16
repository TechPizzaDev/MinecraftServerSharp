using System;
using System.Runtime.Serialization;

namespace MCServerSharp.NBT
{
    public class NbtDepthException : NbtException
    {
        public NbtDepthException()
        {
        }

        public NbtDepthException(string message) : base(message)
        {
        }

        public NbtDepthException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NbtDepthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
