
namespace MCServerSharp.NBT
{
    public static class NbtTypeExtensions
    {
        /// <summary>
        /// Gets whether the type is an array or string (not list).
        /// </summary>
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

        /// <summary>
        /// Gets whether the type is a compound or list.
        /// </summary>
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

        /// <summary>
        /// Gets whether the type is a container or array.
        /// </summary>
        public static bool IsArrayLike(this NbtType tagType)
        {
            return tagType.IsContainer() || tagType.IsArray();
        }

        /// <summary>
        /// Gets whether the type is a data primitive (not array-like).
        /// </summary>
        public static bool IsPrimitive(this NbtType tagType)
        {
            if (tagType == NbtType.Undefined)
                return false;

            return !tagType.IsArrayLike();
        }
    }
}
