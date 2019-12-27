using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Transforms network messages into comprehensible packets.
    /// </summary>
    public class NetPacketDecoder : NetPacketCoder
    {
        public void RegisterClientPacketsFromCallingAssembly()
        {
            RegisterPacketFromCallingAssembly(x => x.IsClientPacket);
        }

        public readonly struct PacketDecoderEntry
        {

        }
    }
}
