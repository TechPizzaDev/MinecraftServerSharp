using System.Buffers;
using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.Net.Packets
{
    [PacketStruct(ClientPacketId.LegacyServerListPing)]
    public readonly struct ClientLegacyServerListPing
    {
        private readonly byte _pluginIdentifier;
        private readonly string _magicString;
        private readonly short _dataLength;
        private readonly byte _protocolVersion;
        private readonly string _hostname;
        private readonly int _port;

        public byte PluginIdentifier => _pluginIdentifier;
        public string MagicString => _magicString;
        public short DataLength => _dataLength;
        public byte ProtocolVersion => _protocolVersion;
        public string Hostname => _hostname;
        public int Port => _port;

        [PacketConstructor]
        public ClientLegacyServerListPing(NetBinaryReader reader, out OperationStatus status) : this()
        {
            status = reader.Read(out _pluginIdentifier);
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

            status = reader.Read(magicStringLength, out _magicString);
            if (status != OperationStatus.Done) 
                return;

            status = reader.Read(out _dataLength);
            if (status != OperationStatus.Done) 
                return;

            status = reader.Read(out _protocolVersion);
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

            status = reader.Read(hostnameLength, out _hostname);
            if (status != OperationStatus.Done)
                return;

            status = reader.Read(out _port);
        }
    }
}
