using System;

namespace MinecraftServerSharp.Utility
{
    public interface IReadOnlyMemory : IElementContainer
    {
        ReadOnlySpan<byte> Span { get; }
    }

    public interface IReadOnlyMemory<T> : IReadOnlyMemory
    {
        new ReadOnlySpan<T> Span { get; }
    }
}
