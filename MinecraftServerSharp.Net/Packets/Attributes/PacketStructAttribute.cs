using System;

namespace MinecraftServerSharp.Net.Packets
{
    /// <summary>
    /// Defines metadata for a packet struct type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class PacketStructAttribute : Attribute
    {
        public int PacketId { get; }

        public bool IsClientPacket { get; }
        public bool IsServerPacket { get; }
        public bool IsLoopbackPacket => IsClientPacket && IsServerPacket;

        public PacketStructAttribute(int packetId)
        {
            PacketId = packetId;
        }

        public PacketStructAttribute(ClientPacketId packetId) : this((int)packetId)
        {
            IsClientPacket = true;
        }

        public PacketStructAttribute(ServerPacketId packetId) : this((int)packetId)
        {
            IsServerPacket = true;
        }

        public PacketStructAttribute(LoopbackPacketId packetId) : this((int)packetId)
        {
            IsClientPacket = true;
            IsServerPacket = true;
        }
    }
}