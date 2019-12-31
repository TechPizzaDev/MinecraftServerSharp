using System;
using System.IO;
using MinecraftServerSharp.Network.Data;

namespace MinecraftServerSharp.Network.Packets
{
    [PacketStruct(ServerPacketID.LegacyServerListPong, ProtocolState.Handshaking)]
    public readonly struct ServerLegacyServerListPong : IWritablePacket
    {
        public int ProtocolVersion { get; }
        public MinecraftVersion MinecraftVersion { get; }
        public string MessageOfTheDay { get; }
        public int CurrentPlayerCount { get; }
        public int MaxPlayers { get; }

        public ServerLegacyServerListPong(
            int protocolVersion, MinecraftVersion minecraftVersion, 
            string messageOfTheDay, int currentPlayerCount, int maxPlayers)
        {
            ProtocolVersion = protocolVersion;
            MinecraftVersion = minecraftVersion ?? throw new ArgumentNullException(nameof(minecraftVersion));
            MessageOfTheDay = messageOfTheDay ?? throw new ArgumentNullException(nameof(messageOfTheDay));
            CurrentPlayerCount = currentPlayerCount;
            MaxPlayers = maxPlayers;
        }

        public void Write(NetBinaryWriter writer)
        {
            void WriteField(string value)
            {
                writer.WriteRaw(value);
                writer.Write((short)0); // null char delimeter
            }

            writer.Write((byte)0xff);
            writer.Write((short)0); // reserved space for message length

            long startPos = writer.Position;
            WriteField("§1");
            WriteField(ProtocolVersion.ToString());
            WriteField(MinecraftVersion.ToString());
            WriteField(MessageOfTheDay.ToString());
            WriteField(CurrentPlayerCount.ToString());
            writer.WriteRaw(MaxPlayers.ToString()); // don't write null char delimeter

            int byteLength = (int)(writer.Position - startPos);
            writer.Seek((int)(startPos - sizeof(short)), SeekOrigin.Begin);
            writer.Write((short)(byteLength / sizeof(char))); // write in reserved space 
        }
    }
}
