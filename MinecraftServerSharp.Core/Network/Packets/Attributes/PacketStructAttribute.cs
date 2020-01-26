using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Defines metadata for a packet struct type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class PacketStructAttribute : Attribute
    {
        public int PacketID { get; }

        public bool IsClientPacket { get; }
        public bool IsServerPacket { get; }

        public PacketStructAttribute(int packetID)
        {
            PacketID = packetID;
        }

        public PacketStructAttribute(ClientPacketID packetID) : this((int)packetID)
        {
            IsClientPacket = true;
        }

        public PacketStructAttribute(ServerPacketID packetID) : this((int)packetID)
        {
            IsServerPacket = true;
        }
    }
}