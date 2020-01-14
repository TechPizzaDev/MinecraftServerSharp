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

        public PacketStructAttribute(ClientPacketID packetID)
        {
            ClientPacketID = packetID;
            ServerPacketID = ServerPacketID.Undefined;
        }

        public PacketStructAttribute(ServerPacketID packetID)
        {
            ClientPacketID = ClientPacketID.Undefined;
            ServerPacketID = packetID;
        }
    }
}