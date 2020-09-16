
namespace MCServerSharp.NBT
{
    public static class NbtTypeExtensions
    {
        public static bool IsArray(this NbtType tagType)
        {
            switch (tagType)
            {
                case NbtType.String:
                case NbtType.ByteArray:
                case NbtType.IntArray:
                case NbtType.LongArray:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsContainer(this NbtType tagType)
        {
            switch (tagType)
            {
                case NbtType.List:
                case NbtType.Compound:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsArrayLike(this NbtType tagType)
        {
            return tagType.IsContainer() || tagType.IsArray();
        }

        public static bool IsPrimitive(this NbtType tagType)
        {
            if (tagType == NbtType.Null)
                return false;

            return !tagType.IsArrayLike();
        }
    }
}
