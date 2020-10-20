using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.LoginSuccess)]
    public readonly struct ServerLoginSuccess
    {
        [DataProperty(0)] public UUID UUID { get; }
        [DataProperty(1)] public Utf8String Username { get; }

        public ServerLoginSuccess(UUID uuid, Utf8String username)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            UUID = uuid;
        }
    }
}
