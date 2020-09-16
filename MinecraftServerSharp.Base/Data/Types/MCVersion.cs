using System.Globalization;

namespace MCServerSharp
{
    public class MCVersion
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public MCVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public override string ToString()
        {
            var provider = CultureInfo.CurrentCulture;
            return string.Join(
                '.', Major.ToString(provider), Minor.ToString(provider), Patch.ToString(provider));
        }
    }
}
