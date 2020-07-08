using System.Buffers;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketId.LegacyServerListPing)]
    public readonly struct ClientLegacyServerListPing
    {
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly byte PluginIdentifier;
        public readonly string MagicString;
        public readonly short DataLength;
        public readonly byte ProtocolVersion;
        public readonly string Hostname;
        public readonly int Port;
#pragma warning restore CA1051

        [PacketConstructor]
        public ClientLegacyServerListPing(NetBinaryReader reader, out OperationStatus status) : this()
        {
            status = reader.Read(out PluginIdentifier);
            if (status != OperationStatus.Done) 
                return;

            status = reader.Read(out short magicStringLength);
            if (status != OperationStatus.Done) 
                return;

            if (magicStringLength != 11)
            {
                status = OperationStatus.InvalidData;
                return;
            }

            status = reader.Read(magicStringLength, out MagicString);
            if (status != OperationStatus.Done) 
                return;

            status = reader.Read(out DataLength);
            if (status != OperationStatus.Done) 
                return;

            status = reader.Read(out ProtocolVersion);
            if (status != OperationStatus.Done) 
                return;

            status = reader.Read(out short hostnameLength);
            if (status != OperationStatus.Done) 
                return;

            if (!StringHelper.IsValidStringLength(hostnameLength))
            {
                status = OperationStatus.InvalidData;
                return;
            }

            status = reader.Read(hostnameLength, out Hostname);
            if (status != OperationStatus.Done)
                return;

            status = reader.Read(out Port);
        }
    }
}
