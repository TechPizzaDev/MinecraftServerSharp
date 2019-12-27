using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Defines metadata for a packet struct type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class PacketStructAttribute : Attribute
    {
        public ClientPacketID ClientPacketID { get; }
        public ServerPacketID ServerPacketID { get; }

        public ProtocolState ProtocolState { get; }

        private PacketStructAttribute(ProtocolState state)
        {
            switch (state)
            {
                case ProtocolState.Handshaking:
                case ProtocolState.Status:
                case ProtocolState.Login:
                case ProtocolState.Play:
                    break;

                default:
                case ProtocolState.Undefined:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
            ProtocolState = state;
        }

        public PacketStructAttribute(ClientPacketID packetID, ProtocolState state) : this(state)
        {
            ClientPacketID = packetID;
            ServerPacketID = ServerPacketID.Undefined;
        }

        public PacketStructAttribute(ServerPacketID packetID, ProtocolState state) : this(state)
        {
            ClientPacketID = ClientPacketID.Undefined;
            ServerPacketID = packetID;
        }
    }
}