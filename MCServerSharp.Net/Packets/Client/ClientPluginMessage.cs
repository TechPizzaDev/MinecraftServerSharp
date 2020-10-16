using System;
using System.Buffers;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.PluginMessage)]
    public readonly struct ClientPluginMessage
    {
        public Identifier Channel { get; }
        public byte[] Data { get; } // TODO: wrap in Recyclable

        public ClientPluginMessage(Identifier channel, byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Channel = channel;
        }

        [PacketConstructor]
        public ClientPluginMessage(NetBinaryReader reader, out OperationStatus status) : this()
        {
            status = reader.Read(out Identifier channel);
            if (status != OperationStatus.Done)
                return;

            Channel = channel;
            Data = reader.ReadBytes((int)reader.Remaining);
        }
    }
}
