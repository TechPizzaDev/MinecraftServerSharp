using MinecraftServerSharp.Data;

namespace MinecraftServerSharp.Network.Packets
{
    public interface IWritablePacket
    {
        void Write(NetBinaryWriter writer);
    }
}
