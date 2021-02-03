using System;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Net
{
    [Serializable]
    public class NetException : Exception
    {
        public ProtocolState ProtocolState { get; }

        public NetException(ProtocolState protocolState) 
        {
            ProtocolState = protocolState;
        }

        public NetException(string message, ProtocolState protocolState) : base(message)
        {
            ProtocolState = protocolState;
        }

        public NetException(string message, Exception inner, ProtocolState protocolState) : base(message, inner)
        {
            ProtocolState = protocolState;
        }

        protected NetException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) 
        {
        }
    }
}
