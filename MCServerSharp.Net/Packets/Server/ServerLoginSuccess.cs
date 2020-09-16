using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.LoginSuccess)]
    public readonly struct ServerLoginSuccess
    {
        [PacketProperty(0)] public Utf8String UUID { get; }
        [PacketProperty(1)] public Utf8String Username { get; }

        public ServerLoginSuccess(Utf8String uuid, Utf8String username)
        {
            UUID = uuid ?? throw new ArgumentNullException(nameof(uuid));
            Username = username ?? throw new ArgumentNullException(nameof(username));
        }
    }
}
