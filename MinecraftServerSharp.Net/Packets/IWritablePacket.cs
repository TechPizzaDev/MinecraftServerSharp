using MinecraftServerSharp.Data.IO;

namespace MinecraftServerSharp.Net.Packets
{
    public interface IWritablePacket
    {
        void Write(NetBinaryWriter writer);
    }
}
