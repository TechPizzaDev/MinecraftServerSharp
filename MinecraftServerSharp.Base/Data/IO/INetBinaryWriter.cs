using System;

namespace MinecraftServerSharp.Data.IO
{
    public interface INetBinaryWriter : ISeekable
    {
        public void Write(ReadOnlySpan<byte> buffer);
        public void Write(bool value);
        public void Write(sbyte value);
        public void Write(byte value);
        public void Write(short value);
        public void Write(ushort value);
        public void Write(int value);
        public void Write(long value);

        public void WriteVar(int value);
        public void WriteVar(long value);

        public void Write(float value);
        public void Write(double value);

        public void Write(string value);
        public void WriteRaw(string value);
        public void Write(Utf8String value);
        public void WriteRaw(Utf8String value);
    }
}
