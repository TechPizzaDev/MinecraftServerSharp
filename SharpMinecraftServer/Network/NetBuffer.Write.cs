using System;

namespace SharpMinecraftServer.Network
{
    public partial class NetBuffer
    {
        public void Write(ulong value) => _writer.Write(value);
        public void Write(uint value) => _writer.Write(value);
        public void Write(ushort value) => _writer.Write(value);
        public void Write(string value) => _writer.Write(value);
        public void Write(float value) => _writer.Write(value);
        public void Write(sbyte value) => _writer.Write(value);
        public void Write(ReadOnlySpan<char> chars) => _writer.Write(chars);
        public void Write(ReadOnlySpan<byte> buffer) => _writer.Write(buffer);
        public void Write(long value) => _writer.Write(value);
        public void Write(int value) => _writer.Write(value);
        public void Write(double value) => _writer.Write(value);
        public void Write(decimal value) => _writer.Write(value);

        public void Write(char[] chars, int index, int count) => _writer.Write(chars, index, count);
        public void Write(char[] chars) => _writer.Write(chars);

        public void Write(byte[] buffer, int index, int count) => _writer.Write(buffer, index, count);
        public void Write(byte[] buffer) => _writer.Write(buffer);

        public void Write(byte value) => _writer.Write(value);
        public void Write(bool value) => _writer.Write(value);
        public void Write(short value) => _writer.Write(value);
        public void Write(char value) => _writer.Write(value);

        public void WriteVar(int value) => _writer.WriteVar(value);
        public void WriteVar(long value) => _writer.WriteVar(value);
    }
}
