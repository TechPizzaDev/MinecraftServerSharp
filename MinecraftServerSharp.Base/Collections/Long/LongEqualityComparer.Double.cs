
namespace MinecraftServerSharp.Collections
{
    public partial class LongEqualityComparer<T>
    {
        private class LongDoubleComparer : LongEqualityComparer<double>
        {
            public override unsafe long GetLongHashCode(double obj)
            {
                // Ensure that 0 and -0 have the same hash code
                if (obj == 0)
                    return 0;

                return *(long*)&obj;
            }
        }
    }
}
