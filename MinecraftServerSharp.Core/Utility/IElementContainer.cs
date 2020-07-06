using System;

namespace MinecraftServerSharp.Utility
{
    public interface IElementContainer : IDisposable
    {
        /// <summary>
        /// Gets the amount of elements within the container.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the size of one element in bytes.
        /// </summary>
        int ElementSize { get; }
    }
}
