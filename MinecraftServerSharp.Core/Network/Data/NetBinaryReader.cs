using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using MinecraftServerSharp.DataTypes;

namespace MinecraftServerSharp.Network.Data
{
    public readonly struct NetBinaryReader
    {
        public Stream BaseStream { get; }

        public long Position { get => BaseStream.Position; set => BaseStream.Position = value; }
        public long Length => BaseStream.Length;
        public long Remaining => Length - Position;

        public NetBinaryReader(Stream stream) => BaseStream = stream;

        public long Seek(int offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public int TryRead(Span<byte> buffer)
        {
            return BaseStream.Read(buffer);
        }

        public int TryRead()
        {
            return BaseStream.ReadByte();
        }

        public ReadCode Read(Span<byte> buffer)
        {
            int read = BaseStream.Read(buffer);
            if (read != buffer.Length)
                return ReadCode.EndOfStream;
            return ReadCode.Ok;
        }

        public ReadCode Read(out bool value)
        {
            if (Remaining < sizeof(bool))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            value = TryRead() != 0;
            return ReadCode.Ok;
        }

        public ReadCode Read(out sbyte value)
        {
            if (Remaining < sizeof(sbyte))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            value = (sbyte)TryRead();
            return ReadCode.Ok;
        }

        public ReadCode Read(out byte value)
        {
            if (Remaining < sizeof(byte))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            value = (byte)TryRead();
            return ReadCode.Ok;
        }

        public ReadCode Read(out short value)
        {
            if(Remaining < sizeof(short))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            this.Read(buffer);
            value = BinaryPrimitives.ReadInt16BigEndian(buffer);
            return ReadCode.Ok;
        }

        public ReadCode Read(out ushort value)
        {
            if (Remaining < sizeof(ushort))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            this.Read(buffer);
            value = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            return ReadCode.Ok;
        }

        public ReadCode Read(out int value)
        {
            if (Remaining < sizeof(int))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            this.Read(buffer);
            value = BinaryPrimitives.ReadInt32BigEndian(buffer);
            return ReadCode.Ok;
        }

        public ReadCode Read(out long value)
        {
            if (Remaining < sizeof(long))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            this.Read(buffer);
            value = BinaryPrimitives.ReadInt64BigEndian(buffer);
            return ReadCode.Ok;
        }

        public ReadCode Read(out float value)
        {
            var code = Read(out int intValue);
            if (code != ReadCode.Ok)
            {
                value = default;
                return code;
            }
            value = BitConverter.Int32BitsToSingle(intValue);
            return ReadCode.Ok;
        }

        public ReadCode Read(out double value)
        {
            var code = Read(out long longValue);
            if (code != ReadCode.Ok)
            {
                value = default;
                return code;
            }
            value = BitConverter.Int64BitsToDouble(longValue);
            return ReadCode.Ok;
        }

        public ReadCode Read(out VarInt value, out int bytes)
        {
            return VarInt.TryDecode(BaseStream, out value, out bytes);
        }

        public ReadCode Read(out VarInt value) => Read(out value, out _);

        public ReadCode Read(out VarLong value, out int bytes)
        {
            return VarLong.TryDecode(BaseStream, out value, out bytes);
        }

        #region ReadUtf16String

        public ReadCode ReadUtf16String(out string value)
        {
            var code = Read(out VarInt byteCount, out int lengthBytes);
            if (code != ReadCode.Ok)
            {
                value = default;
                return code;
            }
            if (lengthBytes > 3)
            {
                value = default;
                return ReadCode.InvalidData;
            }

            int length = byteCount / sizeof(char);
            return ReadUtf16String(length, out value);
        }
   
        // TODO: put this under an unsafe conditional
        private unsafe struct StringReadState
        {
            private ReadCode* _codeOutput;

            public NetBinaryReader Reader { get; }
            public ReadCode Code { get => *_codeOutput; set => *_codeOutput = value; }

            public StringReadState(NetBinaryReader reader, ReadCode* codeOutput)
            {
                Reader = reader;
                _codeOutput = codeOutput;
            }
        }

        public unsafe ReadCode ReadUtf16String(int length, out string value)
        {
            if (!NetTextHelper.IsValidStringByteLength(length * sizeof(char)))
            {
                value = default;
                return ReadCode.InvalidData;
            }

            var code = ReadCode.Ok;
            var readState = new StringReadState(this, &code);

            value = string.Create(length, readState, (output, state) =>
            {
                // We can use the string as the backing buffer.
                var outputBytes = MemoryMarshal.AsBytes(output);
                if ((state.Code = state.Reader.Read(outputBytes)) != ReadCode.Ok)
                    return;

                if (NetTextHelper.BigUtf16.GetChars(outputBytes, output) != output.Length)
                    state.Code = ReadCode.InvalidData;
            });
            return code;
        }

        #endregion

        #region ReadString

        public ReadCode Read(out Utf8String value)
        {
            var code = Read(out VarInt byteCount, out int lengthBytes);
            if (code != ReadCode.Ok)
            {
                value = default;
                return code;
            }
            if (lengthBytes > 3)
            {
                value = default;
                return ReadCode.InvalidData;
            }

            int length = byteCount / sizeof(byte);
            return ReadString(length, out value);
        }

        public unsafe ReadCode ReadString(int length, out Utf8String value)
        {
            // length is already in bytes
            NetTextHelper.AssertValidStringByteLength(length);

            var code = ReadCode.Ok;
            var readState = new StringReadState(this, &code);

            value = Utf8String.Create(length, readState, (output, state) =>
            {
                // We can use the string as the backing buffer.
                state.Code = state.Reader.Read(output);
            });
            return code;
        }

        #endregion
    }
}
