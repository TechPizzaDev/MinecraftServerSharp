using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace MCServerSharp.Data.IO
{
    // TODO: add buffering
    public readonly struct NetBinaryReader
    {
        public Stream BaseStream { get; }
        public NetBinaryOptions Options { get; }

        public long Length => BaseStream.Length;
        public long Remaining => Length - Position;

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public NetBinaryReader(Stream stream, NetBinaryOptions? options = default)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            Options = options ?? NetBinaryOptions.JavaDefault;

            if (!BaseStream.CanRead)
                throw new IOException("The stream is not readable.");
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
                Position -= 1;
            return b;
        }

        public OperationStatus Read(Span<byte> buffer)
        {
            if (Remaining < buffer.Length)
                return OperationStatus.NeedMoreData;

            int read;
            while ((read = ReadBytes(buffer)) > 0)
                buffer = buffer.Slice(read);

            if (buffer.Length > 0)
                // this should not happen if everything else works correctly
                throw new EndOfStreamException();

            return OperationStatus.Done;
        }

        public OperationStatus Read(out bool value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(bool)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            value = buffer[0] != 0;
            return OperationStatus.Done;
        }

        public OperationStatus Read(out sbyte value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            value = (sbyte)buffer[0];
            return OperationStatus.Done;
        }

        public OperationStatus Read(out byte value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(byte)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            value = buffer[0];
            return OperationStatus.Done;
        }

        public OperationStatus Read(out short value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            if (Options.IsBigEndian)
                value = BinaryPrimitives.ReadInt16BigEndian(buffer);
            else
                value = BinaryPrimitives.ReadInt16LittleEndian(buffer);
            return OperationStatus.Done;
        }

        public OperationStatus Read(out ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            if (Options.IsBigEndian)
                value = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            else
                value = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            return OperationStatus.Done;
        }

        public OperationStatus Read(out int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            if (Options.IsBigEndian)
                value = BinaryPrimitives.ReadInt32BigEndian(buffer);
            else
                value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            return OperationStatus.Done;
        }

        public OperationStatus Read(out long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            var status = Read(buffer);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            if (Options.IsBigEndian)
                value = BinaryPrimitives.ReadInt64BigEndian(buffer);
            else
                value = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            return OperationStatus.Done;
        }

        public OperationStatus Read(out float value)
        {
            var status = Read(out int intValue);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            value = BitConverter.Int32BitsToSingle(intValue);
            return OperationStatus.Done;
        }

        public OperationStatus Read(out double value)
        {
            var status = Read(out long intValue);
            if (status != OperationStatus.Done)
            {
                value = default;
                return status;
            }
            value = BitConverter.Int64BitsToDouble(intValue);
            return OperationStatus.Done;
        }





        // TODO: make into extensions

        public OperationStatus Read(out VarInt value, out int bytes)
        {
            return VarInt.TryDecode(BaseStream, out value, out bytes);
        }

        public OperationStatus Read(out VarInt value)
        {
            return Read(out value, out _);
        }

        public OperationStatus Read(out VarLong value, out int bytes)
        {
            return VarLong.TryDecode(BaseStream, out value, out bytes);
        }

        public OperationStatus Read(out VarLong value)
        {
            return Read(out value, out _);
        }

        #region Read(string)

        public OperationStatus Read(out string value)
        {
            var code = Read(out VarInt byteCount, out int lengthBytes);
            if (code != OperationStatus.Done)
            {
                value = string.Empty;
                return code;
            }
            if (lengthBytes > 3)
            {
                value = string.Empty;
                return OperationStatus.InvalidData;
            }

            int length = byteCount / sizeof(char);
            return Read(length, out value);
        }

        public unsafe OperationStatus Read(int length, out string value)
        {
            if (!StringHelper.IsValidStringByteLength(length * sizeof(char)))
            {
                value = string.Empty;
                return OperationStatus.InvalidData;
            }

            var code = OperationStatus.Done;
            var readState = new StringReadState(this, &code);

            value = string.Create(length, readState, (output, state) =>
            {
                // We can use the string as the backing buffer.
                var outputBytes = MemoryMarshal.AsBytes(output);
                if ((state.Code = state.Reader.Read(outputBytes)) != OperationStatus.Done)
                    return;

                StringHelper.BigUtf16.GetChars(outputBytes, output);
            });
            return code;
        }

        #endregion

        #region Read(Utf8String)

        public OperationStatus Read(out Utf8String value)
        {
            var code = Read(out VarInt byteCount, out int lengthBytes);
            if (code != OperationStatus.Done)
            {
                value = default!;
                return code;
            }
            if (lengthBytes > 3)
            {
                value = default!;
                return OperationStatus.InvalidData;
            }

            int length = byteCount / sizeof(byte);
            return Read(length, out value);
        }

        public unsafe OperationStatus Read(int length, out Utf8String value)
        {
            // length is already in bytes
            StringHelper.AssertValidStringByteLength(length);

            var code = OperationStatus.Done;
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
            private OperationStatus* _codeOutput;

            public NetBinaryReader Reader { get; }
            public OperationStatus Code { get => *_codeOutput; set => *_codeOutput = value; }

            public StringReadState(NetBinaryReader reader, OperationStatus* codeOutput)
            {
                Reader = reader;
                _codeOutput = codeOutput;
            }
        }
    }
}
