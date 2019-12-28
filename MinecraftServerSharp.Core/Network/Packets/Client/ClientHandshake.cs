using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.Handshake, ProtocolState.Handshaking)]
    public readonly struct ClientHandshake
    {
        [PacketProperty(0)] public VarInt32 ProtocolVersion { get; }
        [PacketProperty(2)] public ushort ServerPort { get; }
        [PacketProperty(3)] public ProtocolState NextState { get; }

        [PacketProperty(1, MaxLength = 255)] 
        public Utf8String ServerAddress { get; }

        [PacketConstructor]
        public ClientHandshake(
            VarInt32 protocolVersion,
            Utf8String serverAddress,
            ushort serverPort,
            VarInt32 nextState)
        {
            ProtocolVersion = protocolVersion;
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            NextState = nextState.AsEnum<ProtocolState>();
        }
    }
}

