using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace MCServerSharp.IO.Compression
{
    public class ZlibStream : Stream
    {
        private DeflateStream _deflate;
        private bool _leaveOpen;
        private CompressionMode _mode;
        private uint _adlerChecksum = 1;

        public ZlibHeader Header { get; }
        public Stream BaseStream => _deflate.BaseStream;

        public override bool CanRead => _deflate.CanRead;
        public override bool CanSeek => _deflate.CanSeek;
        public override bool CanWrite => _deflate.CanWrite;

        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public ZlibStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            _deflate = new DeflateStream(stream, mode, leaveOpen: true);
            _mode = mode;
            _leaveOpen = leaveOpen;

            if (mode == CompressionMode.Decompress)
            {
                int cmf;
                int flg;
                if ((cmf = BaseStream.ReadByte()) == -1 ||
                    (flg = BaseStream.ReadByte()) == -1)
                    throw new InvalidDataException("Failed to read Zlib header.");

                Header = ZlibHeader.Decode((byte)cmf, (byte)flg);
            }
        }

        public ZlibStream(Stream stream, CompressionMode mode) :
            this(stream, mode, leaveOpen: false)
        {
        }

        // Implies mode = Compress
        public ZlibStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            _deflate = new DeflateStream(stream, compressionLevel, leaveOpen: true);
            _leaveOpen = leaveOpen;

            Header = ZlibHeader.CreateForDeflateStream(compressionLevel);
            BaseStream.Write(stackalloc byte[] {
                Header.GetCMF(),
                Header.GetFLG()
            });
        }

        public ZlibStream(Stream stream, CompressionLevel compressionLevel) :
            this(stream, compressionLevel, leaveOpen: false)
        {
        }

        public override int Read(Span<byte> buffer)
        {
            int read = _deflate.Read(buffer);
            _adlerChecksum = Adler32.Calculate(buffer.Slice(0, read), _adlerChecksum);
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _deflate.ReadAsync(buffer, cancellationToken);
            _adlerChecksum = Adler32.Calculate(buffer.Slice(0, read).Span, _adlerChecksum);
            return read;
        }

        public override int ReadByte()
        {
            Span<byte> buf = stackalloc byte[1];
            if (Read(buf) == -1)
                return -1;
            return buf[0];
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _deflate.Write(buffer);
            _adlerChecksum = Adler32.Calculate(buffer, _adlerChecksum);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _deflate.Write(buffer, offset, count);
            _adlerChecksum = Adler32.Calculate(buffer.AsSpan(offset, count), _adlerChecksum);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _deflate.WriteAsync(buffer, cancellationToken);
            _adlerChecksum = Adler32.Calculate(buffer.Span, _adlerChecksum);
        }

        public override void WriteByte(byte value)
        {
            _deflate.WriteByte(value);
            _adlerChecksum = Adler32.Calculate(stackalloc byte[] { value }, _adlerChecksum);
        }

        public override void Flush()
        {
            _deflate.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _deflate.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override async ValueTask DisposeAsync()
        {
            if (_deflate != null)
            {
                var baseStream = BaseStream;
                await _deflate.DisposeAsync().Unchain();
                _deflate = null!;

                if (_mode == CompressionMode.Compress)
                {
                    byte[] checksumBytes = new byte[sizeof(uint)];
                    BinaryPrimitives.WriteUInt32BigEndian(checksumBytes, _adlerChecksum);
                    await baseStream.WriteAsync(checksumBytes).Unchain();
                }

                if (!_leaveOpen)
                    await baseStream.DisposeAsync().Unchain();
            }
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (_deflate != null)
            {
                var baseStream = BaseStream;
                _deflate.Dispose();
                _deflate = null!;

                if (_mode == CompressionMode.Compress)
                {
                    Span<byte> checksumBytes = stackalloc byte[sizeof(uint)];
                    BinaryPrimitives.WriteUInt32BigEndian(checksumBytes, _adlerChecksum);
                    baseStream.Write(checksumBytes);
                }

                if (!_leaveOpen)
                    baseStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
