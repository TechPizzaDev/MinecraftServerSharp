using System;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Gives access to transforms that turn packets into network messages.
    /// </summary>
    public class NetPacketEncoder : NetPacketCoder
    {
        public delegate void PacketWriterDelegate<in TPacket>(TPacket packet, NetBinaryWriter writer);

        public void RegisterServerPacketTypesFromCallingAssembly()
        {
            RegisterPacketTypesFromCallingAssembly(x => x.IsServerPacket);
        }

        public PacketWriterDelegate<TPacket> GetPacketWriter<TPacket>()
        {
            return (PacketWriterDelegate<TPacket>)GetPacketCoder(typeof(TPacket));
        }

        protected override void PreparePacketType(PacketStructInfo structInfo)
        {
            throw new NotImplementedException();
        }
    }
}
