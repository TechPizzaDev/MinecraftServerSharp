
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace MinecraftServerSharp.Utility
{
    /// <summary>
    /// <see cref="MemoryStream"/> implementation that deals with pooling and managing memory streams 
    /// which use potentially large buffers.
    /// </summary>
    /// <remarks>
    /// This class works in tandem with the <see cref="RecyclableMemoryManager"/> to supply 
    /// <see cref="ChunkedMemoryStream"/> objects to callers, while avoiding these specific problems:
    /// <list type="bullet">
    /// <item>
    /// LOH allocations - since all large buffers are pooled, they will never incur a Gen2 GC.
    /// </item>
    /// <item>
    /// Memory waste - A standard memory stream doubles its size when it runs out of room. 
    /// This leads to exponential memory growth as each stream approaches the maximum allowed size.
    /// </item>
    /// <item>
    /// Memory copying - Each time a <see cref="MemoryStream"/> grows, 
    /// all the bytes are copied into new buffers. This implementation only copies the 
    /// bytes when <see cref="MemoryStream.GetBuffer"/> is called.
    /// </item>
    /// <item>
    /// Memory fragmentation - By using homogeneous buffer sizes, it ensures that blocks of memory
    /// can be easily reused.
    /// </item>
    /// </list>
    /// 
    /// The stream is implemented on top of a series of uniformly-sized blocks. 
    /// As the stream's length grows, additional blocks are retrieved from the memory manager. 
    /// It is these blocks that are pooled, not the stream object itself.
    /// 
    /// The biggest wrinkle in this implementation is when <see cref="MemoryStream.GetBuffer"/> is called.
    /// This requires a single contiguous buffer. 
    /// If only a single block is in use, then that block is returned. 
    /// If multiple blocks are in use, we retrieve a larger buffer from the memory manager. 
    /// These large buffers are also pooled,  split by size--they are multiples/exponentials 
    /// of a chunk size (1 MB by default).
    /// 
    /// Once a large buffer is assigned to the stream the blocks are NEVER again used for this stream. 
    /// All operations take place on the large buffer. The large buffer can be replaced by a larger 
    /// buffer from the pool as needed. All blocks and large buffers are maintained in the stream 
    /// until the stream is disposed (unless AggressiveBufferReturn is enabled in the stream manager).
    /// </remarks>
    public sealed class ChunkedMemoryStream : MemoryStream
    {
        private const long MaxStreamLength = int.MaxValue;

        /// <summary>
        /// All of these blocks must be the same size
        /// </summary>
        private readonly List<byte[]> _blocks = new List<byte[]>(1);

        private readonly Guid _id;
        private readonly RecyclableMemoryManager _memoryManager;
        private readonly string? _tag;

        private int _length;
        private int _position;

        /// <summary>
        /// This list is used to store buffers once they're replaced by something larger.
        /// This is for the cases where you have users of this class that may hold onto the buffers longer
        /// than they should and you want to prevent race conditions which could corrupt the data.
        /// </summary>
        private List<byte[]>? _dirtyBuffers;

        // long to allow Interlocked.Read (for .NET Standard 1.4 compat)
        private long _disposedState;

        /// <summary>
        /// This is only set by GetBuffer() if the necessary buffer is larger than a single block size, or on
        /// construction if the caller immediately requests a single large buffer.
        /// </summary>
        /// <remarks>If this field is non-null, it contains the concatenation of the bytes found in the individual
        /// blocks. Once it is created, this (or a larger) largeBuffer will be used for the life of the stream.
        /// </remarks>
        private byte[]? _largeBuffer;

        public int BlockSize => MemoryManager.BlockSize;

        public int BlockCount => _blocks.Count;

        /// <summary>
        /// Unique identifier for this stream across it's entire lifetime
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal Guid Id
        {
            get
            {
                AssertNotDisposed();
                return _id;
            }
        }

        /// <summary>
        /// A temporary identifier for the current usage of this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public string? Tag
        {
            get
            {
                AssertNotDisposed();
                return _tag;
            }
        }

        /// <summary>
        /// Gets the memory manager being used by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal RecyclableMemoryManager MemoryManager
        {
            get
            {
                AssertNotDisposed();
                return _memoryManager;
            }
        }

        /// <summary>
        /// Callstack of the constructor. It is only set if MemoryManager.GenerateCallStacks is true,
        /// which should only be in debugging situations.
        /// </summary>
        internal string? AllocationStack { get; }

        /// <summary>
        /// Callstack of the Dispose call. It is only set if MemoryManager.GenerateCallStacks is true,
        /// which should only be in debugging situations.
        /// </summary>
        internal string? DisposeStack { get; private set; }

        #region Constructors

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        public ChunkedMemoryStream(RecyclableMemoryManager memoryManager)
            : this(memoryManager, Guid.NewGuid(), null, 0, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        public ChunkedMemoryStream(RecyclableMemoryManager memoryManager, Guid id)
            : this(memoryManager, id, null, 0, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        public ChunkedMemoryStream(RecyclableMemoryManager memoryManager, string? tag)
            : this(memoryManager, Guid.NewGuid(), tag, 0, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        public ChunkedMemoryStream(RecyclableMemoryManager memoryManager, Guid id, string? tag)
            : this(memoryManager, id, tag, 0, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        public ChunkedMemoryStream(RecyclableMemoryManager memoryManager, string? tag, int requestedSize)
            : this(memoryManager, Guid.NewGuid(), tag, requestedSize, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        public ChunkedMemoryStream(
            RecyclableMemoryManager memoryManager, Guid id, string? tag, int requestedSize)
            : this(memoryManager, id, tag, requestedSize, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        /// <param name="initialLargeBuffer">
        /// An initial buffer to use. 
        /// This buffer will be owned by the stream and returned to the memory manager upon Dispose.
        /// </param>
        internal ChunkedMemoryStream(
            RecyclableMemoryManager memoryManager, Guid id, string? tag, int requestedSize, byte[]? initialLargeBuffer)
            : base(Array.Empty<byte>())
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _id = id;
            _tag = tag;

            if (requestedSize < memoryManager.BlockSize)
                requestedSize = memoryManager.BlockSize;

            if (initialLargeBuffer == null)
                EnsureCapacity(requestedSize);
            else
                _largeBuffer = initialLargeBuffer;

            if (_memoryManager.GenerateCallStacks)
                AllocationStack = Environment.StackTrace;

            RecyclableMemoryManager.Events.Writer.MemoryStreamCreated(_id, _tag, requestedSize);
            _memoryManager.ReportStreamCreated();
        }

        #endregion

        #region Dispose and Finalize

        ~ChunkedMemoryStream()
        {
            Dispose(false);
        }

        /// <summary>
        /// Returns the memory used by this stream back to the pool.
        /// </summary>
        /// <param name="disposing">Whether we're disposing (true), or being called by the finalizer (false)</param>
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly",
            Justification = "We have different disposal semantics, so SuppressFinalize is in a different spot.")]
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposedState, 1, 0) != 0)
            {
                string? doubleDisposeStack = null;
                if (_memoryManager.GenerateCallStacks)
                    doubleDisposeStack = Environment.StackTrace;

                RecyclableMemoryManager.Events.Writer.MemoryStreamDoubleDispose(
                    _id, _tag, AllocationStack, DisposeStack, doubleDisposeStack);
                return;
            }

            RecyclableMemoryManager.Events.Writer.MemoryStreamDisposed(_id, _tag);

            if (_memoryManager.GenerateCallStacks)
                DisposeStack = Environment.StackTrace;

            if (disposing)
            {
                _memoryManager.ReportStreamDisposed();

                GC.SuppressFinalize(this);
            }
            else
            {
                // We're being finalized.
                RecyclableMemoryManager.Events.Writer.MemoryStreamFinalized(_id, _tag, AllocationStack);

                if (AppDomain.CurrentDomain.IsFinalizingForUnload())
                {
                    // If we're being finalized because of a shutdown, don't go any further.
                    // We have no idea what's already been cleaned up. Triggering events may cause
                    // a crash.
                    base.Dispose(disposing);
                    return;
                }

                _memoryManager.ReportStreamFinalized();
            }

            _memoryManager.ReportStreamLength(_length);

            if (_largeBuffer != null)
                _memoryManager.ReturnLargeBuffer(_largeBuffer, _tag);

            if (_dirtyBuffers != null)
                foreach (var buffer in _dirtyBuffers)
                    _memoryManager.ReturnLargeBuffer(buffer, _tag);

            _memoryManager.ReturnBlocks(_blocks, _tag);
            _blocks.Clear();

            base.Dispose(disposing);
        }

        /// <summary>
        /// Equivalent to <see cref="Stream.Dispose()"/>.
        /// </summary>
        public override void Close()
        {
            Dispose(true);
        }

        #endregion

        #region MemoryStream overrides

        /// <summary>
        /// Gets or sets the capacity.
        /// </summary>
        /// <remarks>
        /// Capacity is always in multiples of the memory manager's block size, unless
        /// the large buffer is in use.  Capacity never decreases during a stream's lifetime. 
        /// Explicitly setting the capacity to a lower value than the current value will have no effect. 
        /// This is because the buffers are all pooled by chunks and there's little reason to 
        /// allow stream truncation.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Capacity
        {
            get
            {
                AssertNotDisposed();
                if (_largeBuffer != null)
                    return _largeBuffer.Length;

                long size = (long)_blocks.Count * _memoryManager.BlockSize;
                return (int)Math.Min(int.MaxValue, size);
            }
            set
            {
                // TODO: implement truncation (supplying a lower capacity than the current one)

                AssertNotDisposed();
                EnsureCapacity(value);
            }
        }

        /// <summary>
        /// Gets the number of bytes written to this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override long Length
        {
            get
            {
                AssertNotDisposed();
                return _length;
            }
        }

        /// <summary>
        /// Gets the current position in the stream
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override long Position
        {
            get
            {
                AssertNotDisposed();
                return _position;
            }
            set
            {
                AssertNotDisposed();
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");

                if (value > MaxStreamLength)
                    throw new ArgumentOutOfRangeException(nameof(value), "value cannot be more than " + MaxStreamLength);

                _position = (int)value;
            }
        }

        /// <summary>
        /// Whether the stream can currently read
        /// </summary>
        public override bool CanRead => !IsDisposed;

        /// <summary>
        /// Whether the stream can currently seek
        /// </summary>
        public override bool CanSeek => !IsDisposed;

        /// <summary>
        /// Always false
        /// </summary>
        public override bool CanTimeout => false;

        /// <summary>
        /// Whether the stream can currently write
        /// </summary>
        public override bool CanWrite => !IsDisposed;

        /// <summary>
        /// Returns a single buffer containing the contents of the stream.
        /// The buffer may be longer than the stream length.
        /// </summary>
        /// <returns>A byte[] buffer</returns>
        /// <remarks>
        /// IMPORTANT: Doing a Write() after calling GetBuffer() invalidates the buffer. The old buffer is held onto
        /// until Dispose is called, but the next time GetBuffer() is called, a new buffer from the pool will be required.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override byte[] GetBuffer()
        {
            AssertNotDisposed();

            if (_largeBuffer != null)
                return _largeBuffer;

            if (_blocks.Count == 1)
                return _blocks[0];

            // Buffer needs to reflect the capacity, not the length, because
            // it's possible that people will manipulate the buffer directly
            // and set the length afterward. Capacity sets the expectation
            // for the size of the buffer.
            var newBuffer = _memoryManager.GetLargeBuffer(Capacity, _tag);

            // InternalRead will check for existence of largeBuffer, so make sure we
            // don't set it until after we've copied the data.
            InternalRead(newBuffer.AsSpan(0, _length), 0);
            _largeBuffer = newBuffer;

            if (_blocks.Count > 0 && _memoryManager.AggressiveBufferReturn)
            {
                _memoryManager.ReturnBlocks(_blocks, _tag);
                _blocks.Clear();
            }

            return _largeBuffer;
        }

        /// <summary>
        /// Returns an ArraySegment that wraps a single buffer containing the contents of the stream.
        /// </summary>
        /// <param name="buffer">An ArraySegment containing a reference to the underlying bytes.</param>
        /// <returns>Always returns true.</returns>
        /// <remarks>GetBuffer has no failure modes (it always returns something, even if it's an empty buffer), therefore this method
        /// always returns a valid ArraySegment to the same buffer returned by GetBuffer.</remarks>
        public override bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            AssertNotDisposed();
            buffer = new ArraySegment<byte>(GetBuffer(), 0, (int)Length);
            // GetBuffer has no failure modes, so this should always succeed
            return true;
        }

        /// <summary>
        /// Returns a new array with a copy of the buffer's contents. You should almost certainly be using GetBuffer combined with the Length to 
        /// access the bytes in this stream. Calling ToArray will destroy the benefits of pooled buffers, but it is included
        /// for the sake of completeness.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        [Obsolete("This method has degraded performance vs. GetBuffer and should be avoided.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override byte[] ToArray()
#pragma warning restore CS0809
        {
            AssertNotDisposed();
            var newBuffer = new byte[Length];

            InternalRead(newBuffer.AsSpan(0, _length), 0);
            var stack = _memoryManager.GenerateCallStacks ? Environment.StackTrace : null;
            RecyclableMemoryManager.Events.Writer.MemoryStreamToArray(_id, _tag, stack, 0);
            _memoryManager.ReportStreamToArray();

            return newBuffer;
        }

        /// <summary>
        /// Reads from the current position into the provided buffer
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0</exception>
        /// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return SafeRead(buffer, offset, count, ref _position);
        }

        /// <summary>
        /// Reads from the specified position into the provided buffer
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="streamPosition">Position in the stream to start reading from</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0</exception>
        /// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public int SafeRead(byte[] buffer, int offset, int count, ref int streamPosition)
        {
            AssertNotDisposed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            int amountRead = InternalRead(buffer.AsSpan(offset, count), streamPosition);
            streamPosition += amountRead;
            return amountRead;
        }

        /// <summary>
        /// Reads from the current position into the provided buffer
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Read(Span<byte> buffer)
        {
            return SafeRead(buffer, ref _position);
        }

        /// <summary>
        /// Reads from the specified position into the provided buffer
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="streamPosition">Position in the stream to start reading from</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public int SafeRead(Span<byte> buffer, ref int streamPosition)
        {
            AssertNotDisposed();

            int amountRead = InternalRead(buffer, streamPosition);
            streamPosition += amountRead;
            return amountRead;
        }

        /// <summary>
        /// Writes the buffer to the stream
        /// </summary>
        /// <param name="buffer">Source buffer</param>
        /// <param name="offset">Start position</param>
        /// <param name="count">Number of bytes to write</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is negative</exception>
        /// <exception cref="ArgumentException">buffer.Length - offset is not less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            AssertNotDisposed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(offset), offset, "Offset must be in the range of 0 - buffer.Length-1");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative");

            if (count + offset > buffer.Length)
                throw new ArgumentException("count must be greater than buffer.Length - offset");

            int blockSize = _memoryManager.BlockSize;
            long end = (long)_position + count;
            // Check for overflow
            if (end > MaxStreamLength)
                throw new IOException("Maximum capacity exceeded");

            EnsureCapacity((int)end);

            if (_largeBuffer == null)
            {
                int bytesRemaining = count;
                int bytesWritten = 0;
                var blockOffset = GetBlockOffset(_position);

                while (bytesRemaining > 0)
                {
                    byte[] currentBlock = _blocks[blockOffset.Block];
                    int remainingInBlock = blockSize - blockOffset.Offset;
                    int amountToWriteInBlock = Math.Min(remainingInBlock, bytesRemaining);

                    Buffer.BlockCopy(buffer, offset + bytesWritten, currentBlock, blockOffset.Offset,
                                     amountToWriteInBlock);

                    bytesRemaining -= amountToWriteInBlock;
                    bytesWritten += amountToWriteInBlock;

                    blockOffset.Block++;
                    blockOffset.Offset = 0;
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _largeBuffer, _position, count);
            }
            _position = (int)end;
            _length = Math.Max(_position, _length);
        }

        /// <summary>
        /// Writes the buffer to the stream
        /// </summary>
        /// <param name="source">Source buffer</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void Write(ReadOnlySpan<byte> source)
        {
            AssertNotDisposed();

            int blockSize = _memoryManager.BlockSize;
            long end = (long)_position + source.Length;
            // Check for overflow
            if (end > MaxStreamLength)
                throw new IOException("Maximum capacity exceeded");

            EnsureCapacity((int)end);

            if (_largeBuffer == null)
            {
                var blockOffset = GetBlockOffset(_position);

                while (source.Length > 0)
                {
                    byte[] currentBlock = _blocks[blockOffset.Block];
                    int remainingInBlock = blockSize - blockOffset.Offset;
                    int amountToWriteInBlock = Math.Min(remainingInBlock, source.Length);

                    source.Slice(0, amountToWriteInBlock)
                        .CopyTo(currentBlock.AsSpan(blockOffset.Offset));

                    source = source.Slice(amountToWriteInBlock);

                    blockOffset.Block++;
                    blockOffset.Offset = 0;
                }
            }
            else
            {
                source.CopyTo(_largeBuffer.AsSpan(_position));
            }
            _position = (int)end;
            _length = Math.Max(_position, _length);
        }

        /// <summary>
        /// Returns a useful string for debugging. This should not normally be called in actual production code.
        /// </summary>
        public override string ToString()
        {
            return $"Id = {Id}, Tag = {Tag}, Length = {Length:N0} bytes";
        }

        /// <summary>
        /// Writes a single byte to the current position in the stream.
        /// </summary>
        /// <param name="value">byte value to write</param>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void WriteByte(byte value)
        {
            AssertNotDisposed();
            Span<byte> buffer = stackalloc byte[] { value };
            Write(buffer);
        }

        /// <summary>
        /// Reads a single byte from the current position in the stream.
        /// </summary>
        /// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int ReadByte()
        {
            return SafeReadByte(ref _position);
        }

        /// <summary>
        /// Reads a single byte from the specified position in the stream.
        /// </summary>
        /// <param name="streamPosition">The position in the stream to read from</param>
        /// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public int SafeReadByte(ref int streamPosition)
        {
            AssertNotDisposed();
            if (streamPosition == _length)
                return -1;

            byte value;
            if (_largeBuffer == null)
            {
                var blockOffset = GetBlockOffset(streamPosition);
                value = _blocks[blockOffset.Block][blockOffset.Offset];
            }
            else
            {
                value = _largeBuffer[streamPosition];
            }
            streamPosition++;
            return value;
        }

        /// <summary>
        /// Sets the length of the stream
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value is negative or larger than MaxStreamLength</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void SetLength(long value)
        {
            AssertNotDisposed();
            if (value < 0 || value > MaxStreamLength)
                throw new ArgumentOutOfRangeException(
                    nameof(value), "value must be non-negative and at most " + MaxStreamLength);

            EnsureCapacity((int)value);

            _length = (int)value;
            if (_position > value)
                _position = (int)value;
        }

        /// <summary>
        /// Sets the position to the offset from the seek location
        /// </summary>
        /// <param name="offset">How many bytes to move</param>
        /// <param name="origin">From where</param>
        /// <returns>The new position</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset is larger than MaxStreamLength</exception>
        /// <exception cref="ArgumentException">Invalid seek origin</exception>
        /// <exception cref="IOException">Attempt to set negative position</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            AssertNotDisposed();
            if (offset > MaxStreamLength)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be larger than " + MaxStreamLength);

            int newPosition = origin switch
            {
                SeekOrigin.Begin => (int)offset,
                SeekOrigin.Current => (int)offset + _position,
                SeekOrigin.End => (int)offset + _length,
                _ => throw new ArgumentException("Invalid seek origin", nameof(origin)),
            };
            if (newPosition < 0)
                throw new IOException("Seek before beginning");

            _position = newPosition;
            return _position;
        }

        /// <summary>
        /// Synchronously writes this stream's bytes to the parameter stream.
        /// </summary>
        /// <param name="stream">Destination stream</param>
        /// <remarks>Important: This does a synchronous write, which may not be desired in some situations</remarks>
        public override void WriteTo(Stream stream)
        {
            AssertNotDisposed();
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (_largeBuffer == null)
            {
                int currentBlock = 0;
                int bytesRemaining = _length;

                while (bytesRemaining > 0)
                {
                    int amountToCopy = Math.Min(_blocks[currentBlock].Length, bytesRemaining);
                    stream.Write(_blocks[currentBlock], 0, amountToCopy);

                    bytesRemaining -= amountToCopy;

                    ++currentBlock;
                }
            }
            else
            {
                stream.Write(_largeBuffer, 0, _length);
            }
        }

        #endregion

        public Memory<byte> GetBlock(int index)
        {
            return _blocks[index];
        }

        public void RemoveBlockRange(int startIndex, int count)
        {
            AssertNotDisposed();

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                _memoryManager.ReturnBlock(_blocks[i + startIndex], _tag);

            _blocks.RemoveRange(startIndex, count);

            _length -= count * BlockSize;
        }

        #region Helper Methods

        public bool IsDisposed => Interlocked.Read(ref _disposedState) != 0;

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    $"The stream with Id {_id} and Tag {_tag} is disposed.");
        }

        private int InternalRead(Span<byte> buffer, int fromPosition)
        {
            if (_length - fromPosition <= 0)
                return 0;

            int amountToCopy;

            if (_largeBuffer == null)
            {
                var blockOffset = GetBlockOffset(fromPosition);
                int bytesWritten = 0;
                int bytesRemaining = Math.Min(buffer.Length, _length - fromPosition);

                while (bytesRemaining > 0)
                {
                    amountToCopy = Math.Min(
                        _blocks[blockOffset.Block].Length - blockOffset.Offset,
                        bytesRemaining);

                    _blocks[blockOffset.Block].AsSpan(blockOffset.Offset, amountToCopy)
                        .CopyTo(buffer.Slice(bytesWritten));

                    bytesWritten += amountToCopy;
                    bytesRemaining -= amountToCopy;

                    blockOffset.Block++;
                    blockOffset.Offset = 0;
                }
                return bytesWritten;
            }
            amountToCopy = Math.Min(buffer.Length, _length - fromPosition);
            _largeBuffer.AsSpan(fromPosition, amountToCopy).CopyTo(buffer);
            return amountToCopy;
        }

        public struct BlockOffset
        {
            public int Block;
            public int Offset;

            public BlockOffset(int block, int offset)
            {
                Block = block;
                Offset = offset;
            }
        }

        public BlockOffset GetBlockOffset(int offset)
        {
            return new BlockOffset(offset / BlockSize, offset % BlockSize);
        }

        private void EnsureCapacity(int newCapacity)
        {
            if (newCapacity > _memoryManager.MaximumStreamCapacity && _memoryManager.MaximumStreamCapacity > 0)
            {
                RecyclableMemoryManager.Events.Writer.MemoryStreamOverCapacity(
                    newCapacity, _memoryManager.MaximumStreamCapacity, _tag, AllocationStack);

                throw new InvalidOperationException(
                    "Requested capacity is too large: " + newCapacity +
                    ". Limit is " + _memoryManager.MaximumStreamCapacity);
            }

            if (_largeBuffer != null)
            {
                if (newCapacity > _largeBuffer.Length)
                {
                    var newBuffer = _memoryManager.GetLargeBuffer(newCapacity, _tag);
                    InternalRead(newBuffer.AsSpan(0, _length), 0);
                    ReleaseLargeBuffer();
                    _largeBuffer = newBuffer;
                }
            }
            else
            {
                while (Capacity < newCapacity)
                    _blocks.Add(_memoryManager.GetBlock());
            }
        }

        /// <summary>
        /// Release the large buffer (either stores it for eventual release or returns it immediately).
        /// </summary>
        private void ReleaseLargeBuffer()
        {
            if (_largeBuffer == null)
                throw new InvalidOperationException();

            if (_memoryManager.AggressiveBufferReturn)
            {
                _memoryManager.ReturnLargeBuffer(_largeBuffer, _tag);
            }
            else
            {
                if (_dirtyBuffers == null)
                    // We most likely will only ever need space for one
                    _dirtyBuffers = new List<byte[]>(1);

                _dirtyBuffers.Add(_largeBuffer);
            }

            _largeBuffer = null;
        }

        #endregion
    }
}