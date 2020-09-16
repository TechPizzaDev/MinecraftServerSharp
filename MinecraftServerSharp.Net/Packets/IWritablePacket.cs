using MCServerSharp.Data.IO;

namespace MCServerSharp.Net.Packets
{
    public interface IWritablePacket
    {
        void Write(NetBinaryWriter writer);
    }
}
