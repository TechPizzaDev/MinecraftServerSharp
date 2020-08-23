using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.Network.Packets
{
    public interface IWritablePacket
    {
        void Write(NetBinaryWriter writer);
    }
}
