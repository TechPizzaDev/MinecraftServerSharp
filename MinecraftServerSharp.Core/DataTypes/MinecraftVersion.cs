
namespace MinecraftServerSharp
{
    public class MinecraftVersion
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public MinecraftVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public override string ToString()
        {
            return string.Join('.', Major.ToString(), Minor.ToString(), Patch.ToString());
        }
    }
}
