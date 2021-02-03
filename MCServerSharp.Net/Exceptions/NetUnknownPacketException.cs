using System;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Net
{
    [Serializable]
    public class NetUnknownPacketException : NetException
    {
        public Type? PacketType { get; }
        public int? PacketId { get; }

        public NetUnknownPacketException(ProtocolState protocolState, Type? packetType) : base(protocolState)
        {
            PacketType = packetType;
        }

        public NetUnknownPacketException(string message, ProtocolState protocolState, Type? packetType) :
            base(message, protocolState)
        {
            PacketType = packetType;
        }

        public NetUnknownPacketException(string message, Exception inner, ProtocolState protocolState, int packetId) :
            base(message, inner, protocolState)
        {
            PacketId = packetId;
        }

        public NetUnknownPacketException(ProtocolState protocolState, int packetId) : base(protocolState)
        {
            PacketId = packetId;
        }

        public NetUnknownPacketException(string message, ProtocolState protocolState, int packetId) :
            base(message, protocolState)
        {
            PacketId = packetId;
        }

        public NetUnknownPacketException(string message, Exception inner, ProtocolState protocolState, Type? packetType) :
            base(message, inner, protocolState)
        {
            PacketType = packetType;
        }

        protected NetUnknownPacketException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) 
        {
        }
    }
}
