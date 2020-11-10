namespace MCServerSharp.Net.Packets
{
    public enum DataSerializeMode
    {
        /// <summary>
        /// Chooses <see cref="Serialize"/> by default.
        /// <see cref="Copy"/> is used for elements of known blittable types
        /// (e.g. <see cref="bool"/>, <see cref="byte"/>, <see cref="float"/>, <see cref="int"/>).
        /// </summary>
        Auto,

        /// <summary>
        /// The elements are passed through the serializer
        /// which may invoke <see cref="IDataWritable"/> or 
        /// write the type dynamically.
        /// </summary>
        Serialize,

        /// <summary>
        /// The elements are written as bytes without any extra processing.
        /// </summary>
        Copy
    }
}
