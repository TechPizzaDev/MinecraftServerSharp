
namespace MinecraftServerSharp.NBT
{
    public static class NbtTypeExtensions
    {
        public static bool IsPrimitiveArray(this NbtType tagType)
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
            return tagType.IsContainer() || tagType.IsPrimitiveArray();
        }

        public static bool IsPrimitive(this NbtType tagType)
        {
            if (tagType == NbtType.Null)
                return false;

            return !tagType.IsArrayLike();
        }
    }
}
