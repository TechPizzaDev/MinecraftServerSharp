using System;
using System.IO;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.LegacyServerListPong)]
    public readonly struct ServerLegacyServerListPong : IDataWritable
    {
        public bool IsBeta { get; }
        public int ProtocolVersion { get; }
        public MCVersion MinecraftVersion { get; }
        public string MessageOfTheDay { get; }
        public int CurrentPlayerCount { get; }
        public int MaxPlayers { get; }

        public ServerLegacyServerListPong(
            bool isBeta,
            int protocolVersion, 
            MCVersion minecraftVersion,
            string messageOfTheDay,
            int currentPlayerCount, 
            int maxPlayers)
        {
            IsBeta = isBeta;
            ProtocolVersion = protocolVersion;
            MinecraftVersion = minecraftVersion ?? throw new ArgumentNullException(nameof(minecraftVersion));
            MessageOfTheDay = messageOfTheDay ?? throw new ArgumentNullException(nameof(messageOfTheDay));
            CurrentPlayerCount = currentPlayerCount;
            MaxPlayers = maxPlayers;
        }

        public void Write(NetBinaryWriter writer)
        {
            bool isBeta = IsBeta;
            void WriteField(string value, bool delimit = true)
            {
                writer.WriteRaw(value);
                if (delimit)
                    writer.Write((short)(isBeta ? '§' : 0)); // null char delimeter
            }

            writer.Write((byte)0xff);
            writer.Write((short)0); // reserved space for message length

            long startPos = writer.Position;
            if (!isBeta)
            {
                WriteField("§1");
                WriteField(ProtocolVersion.ToString());
                WriteField(MinecraftVersion.ToString());
            }
            WriteField(MessageOfTheDay.ToString());
            WriteField(CurrentPlayerCount.ToString());
            WriteField(MaxPlayers.ToString(), delimit: false);

            int byteLength = (int)(writer.Position - startPos);
            int reservedSpaceOffset = (int)(startPos - sizeof(short));

            long endPosition = writer.Position;
            writer.Seek(reservedSpaceOffset, SeekOrigin.Begin);
            writer.Write((short)(byteLength / sizeof(char)));

            writer.Seek((int)endPosition, SeekOrigin.Begin);
        }
    }
}
