using System;

namespace MinecraftServerSharp.Network.Data
{
    internal readonly unsafe struct BytePointer
    {
        public byte* Pointer { get; }
        public int Length { get; }

        public BytePointer(byte* pointer, int length)
        {
            Pointer = pointer;
            Length = length;
        }

        public Span<byte> AsSpan() => new Span<byte>(Pointer, Length);
    }
}
