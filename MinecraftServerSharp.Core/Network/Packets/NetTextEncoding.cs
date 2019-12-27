namespace MinecraftServerSharp.Network.Packets
{
    public enum NetTextEncoding
    {
        /// <summary>
        /// Gets an encoding for the UTF-8 format.
        /// </summary>
        Utf8,

        /// <summary>
        /// Gets an encoding for the UTF-16 format using the big endian byte order.
        /// </summary>
        BigUtf16,

        /// <summary>
        /// Gets an encoding for the UTF-32 format using the big endian byte order.
        /// </summary>
        BigUtf32,

        /// <summary>
        /// Gets an encoding for the UTF-16 format using the little endian byte order.
        /// </summary>
        LittleUtf16,

        /// <summary>
        /// Gets an encoding for the UTF-32 format using the little endian byte order.
        /// </summary>
        LittleUtf32,
    }
}
