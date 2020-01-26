using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.Network.Data
{
    public readonly struct NetBinaryReader
    {
        public Stream BaseStream { get; }

        public long Position { get => BaseStream.Position; set => BaseStream.Position = value; }
        public long Length => BaseStream.Length;
        public long Remaining => Length - Position;

        public NetBinaryReader(Stream stream)
        {
            BaseStream = stream;
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public int ReadBytes(Span<byte> buffer)
        {
            return BaseStream.Read(buffer);
        }

        public int ReadByte()
        {
            return BaseStream.ReadByte();
        }

        public int PeekByte()
        {
            int b = ReadByte();
            if (b != -1)
                Seek(-1, SeekOrigin.Current);
            return b;
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
            value = ReadByte() != 0;
            return ReadCode.Ok;
        }

        public ReadCode Read(out sbyte value)
        {
            if (Remaining < sizeof(sbyte))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            value = (sbyte)ReadByte();
            return ReadCode.Ok;
        }

        public ReadCode Read(out byte value)
        {
            if (Remaining < sizeof(byte))
            {
                value = default;
                return ReadCode.EndOfStream;
            }
            value = (byte)ReadByte();
            return ReadCode.Ok;
        }

        public ReadCode Read(out short value)
        {
            if (Remaining < sizeof(short))
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

        public ReadCode Read(out VarInt value, out int bytes) => VarInt.TryDecode(BaseStream, out value, out bytes);

        public ReadCode Read(out VarInt value) => Read(out value, out _);

        public ReadCode Read(out VarLong value, out int bytes) => VarLong.TryDecode(BaseStream, out value, out bytes);

        public ReadCode Read(out VarLong value) => Read(out value, out _);

        #region Read(string)

        public ReadCode Read(out string value)
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
            return Read(length, out value);
        }

        public unsafe ReadCode Read(int length, out string value)
        {
            if (!StringHelper.IsValidStringByteLength(length * sizeof(char)))
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

                StringHelper.BigUtf16.GetChars(outputBytes, output);
            });
            return code;
        }

        #endregion

        #region Read(Utf8String)

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
            return Read(length, out value);
        }

        public unsafe ReadCode Read(int length, out Utf8String value)
        {
            // length is already in bytes
            StringHelper.AssertValidStringByteLength(length);

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
    }
}
