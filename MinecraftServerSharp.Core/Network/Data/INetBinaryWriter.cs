using System;
using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Data
{
    public interface INetBinaryWriter : ISeekable
    {
        public void Write(ulong value);
        public void Write(uint value);
        public void Write(ushort value);
        public void Write(string value);
        public void Write(float value);
        public void Write(sbyte value);
        public void Write(ReadOnlySpan<char> chars);
        public void Write(ReadOnlySpan<byte> buffer);
        public void Write(long value);
        public void Write(int value);
        public void Write(double value);
        public void Write(decimal value);
        public void Write(byte value);
        public void Write(bool value);
        public void Write(short value);
        public void Write(char value);

        public void WriteVar(VarInt32 value);
        public void WriteVar(VarInt64 value);
    }
}
