using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ClientPacketID.Handshake, ProtocolState.Handshaking)]
    public readonly struct ClientHandshake
    {
        [PacketProperty(0)] 
        public VarInt32 ProtocolVersion { get; }
        
        [PacketProperty(1, MaxLength = 255)] 
        public string ServerAddress { get; }
        
        [PacketProperty(2)] 
        public ushort ServerPort { get; }

        [PacketProperty(3, UnderlyingType = typeof(VarInt32))]
        public ProtocolState NextState { get; }

        [PacketConstructor]
        public ClientHandshake(
            VarInt32 protocolVersion,
            string serverAddress,
            ushort serverPort,
            ProtocolState nextState)
        {
            ProtocolVersion = protocolVersion;
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            NextState = nextState;
        }
    }
}

