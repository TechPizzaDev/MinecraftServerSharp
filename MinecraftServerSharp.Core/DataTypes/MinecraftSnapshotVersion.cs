
namespace MinecraftServerSharp.Network.Packets
{
    public class MinecraftSnapshotVersion : MinecraftVersion
    {
        public int Year { get; }
        public byte Week { get; }
        public int Revision { get; }

        public MinecraftSnapshotVersion(
            int major, int minor,
            int year, byte week, int revision) : base(major, minor, 0)
        {
            Year = year;
            Week = week;
            Revision = revision;
        }
    }
}
