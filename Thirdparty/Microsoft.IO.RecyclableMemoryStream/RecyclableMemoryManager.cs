using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MinecraftServerSharp.Utility
{
    /// <summary>
    /// Manages pools of array objects.
    /// </summary>
    /// <remarks>
    /// There are two pools managed in here. The small pool contains same-sized buffers that are handed to streams
    /// as they write more data.
    ///
    /// For scenarios that need to call GetBuffer(), the large pool contains buffers of various sizes, all
    /// multiples/exponentials of LargeBufferMultiple (1 MB by default). They are split by size to avoid overly-wasteful buffer
    /// usage. There should be far fewer 8 MB buffers than 1 MB buffers, for example.
    /// </remarks>
    public partial class RecyclableMemoryManager
    {
        /// <summary>
        /// Generic delegate for handling events without any arguments.
        /// </summary>
        public delegate void EventHandler();

        /// <summary>
        /// Delegate for handling large buffer discard reports.
        /// </summary>
        /// <param name="reason">Reason the buffer was discarded.</param>
        public delegate void LargeBufferDiscardedEventHandler(Events.MemoryStreamDiscardReason reason);

        /// <summary>
        /// Delegate for handling reports of stream size when streams are allocated
        /// </summary>
        /// <param name="bytes">Bytes allocated.</param>
        public delegate void StreamLengthReportHandler(long bytes);

        /// <summary>
        /// Delegate for handling periodic reporting of memory use statistics.
        /// </summary>
        /// <param name="smallPoolInUseBytes">Bytes currently in use in the small pool.</param>
        /// <param name="smallPoolFreeBytes">Bytes currently free in the small pool.</param>
        /// <param name="largePoolInUseBytes">Bytes currently in use in the large pool.</param>
        /// <param name="largePoolFreeBytes">Bytes currently free in the large pool.</param>
        public delegate void UsageReportEventHandler(
            long smallPoolInUseBytes, long smallPoolFreeBytes, long largePoolInUseBytes, long largePoolFreeBytes);

        public const int DefaultBlockSize = 80 * 1024;
        public const int DefaultLargeBufferMultiple = 1024 * 1024;
        public const int DefaultMaximumBufferSize = 16 * 1024 * 1024;

        private readonly long[] _largeBufferFreeSize;
        private readonly long[] _largeBufferInUseSize;

        /// <summary>
        /// pools[0] = 1x largeBufferMultiple buffers
        /// pools[1] = 2x largeBufferMultiple buffers
        /// pools[2] = 3x(multiple)/4x(exponential) largeBufferMultiple buffers
        /// etc., up to maximumBufferSize
        /// </summary>
        private readonly ConcurrentStack<byte[]>[] _largePools;

        private readonly ConcurrentStack<byte[]> _smallPool;

        private long _smallPoolFreeSize;
        private long _smallPoolInUseSize;

        private static object _defaultInitMutex = new object();
        private static RecyclableMemoryManager _default;

        public static RecyclableMemoryManager Default
        {
            get
            {
                if (_default != null)
                    return _default;

                lock (_defaultInitMutex)
                {
                    if (_default == null)
                    {
                        _default = new RecyclableMemoryManager();
                        _default.AggressiveBufferReturn = true;
                    }
                    return _default;
                }
            }
        }

        #region Constructors

        /// <summary>
        /// Initializes the memory manager with the default block/buffer specifications.
        /// </summary>
        public RecyclableMemoryManager()
            : this(DefaultBlockSize, DefaultLargeBufferMultiple, DefaultMaximumBufferSize, false)
        {
        }

        /// <summary>
        /// Initializes the memory manager with the given block requiredSize.
        /// </summary>
        /// <param name="blockSize">Size of each block that is pooled. Must be > 0.</param>
        /// <param name="largeBufferMultiple">Each large buffer will be a multiple of this value.</param>
        /// <param name="maximumBufferSize">Buffers larger than this are not pooled</param>
        /// <exception cref="ArgumentOutOfRangeException">blockSize is not a positive number, or largeBufferMultiple is not a positive number, or maximumBufferSize is less than blockSize.</exception>
        /// <exception cref="ArgumentException">maximumBufferSize is not a multiple of largeBufferMultiple</exception>
        public RecyclableMemoryManager(int blockSize, int largeBufferMultiple, int maximumBufferSize)
            : this(blockSize, largeBufferMultiple, maximumBufferSize, false)
        {
        }

        /// <summary>
        /// Initializes the memory manager with the given block requiredSize.
        /// </summary>
        /// <param name="blockSize">Size of each block that is pooled. Must be > 0.</param>
        /// <param name="largeBufferMultiple">Each large buffer will be a multiple/exponential of this value.</param>
        /// <param name="maximumBufferSize">Buffers larger than this are not pooled</param>
        /// <param name="useExponentialLargeBuffer">Switch to exponential large buffer allocation strategy</param>
        /// <exception cref="ArgumentOutOfRangeException">blockSize is not a positive number, or largeBufferMultiple is not a positive number, or maximumBufferSize is less than blockSize.</exception>
        /// <exception cref="ArgumentException">maximumBufferSize is not a multiple/exponential of largeBufferMultiple</exception>
        public RecyclableMemoryManager(int blockSize, int largeBufferMultiple, int maximumBufferSize, bool useExponentialLargeBuffer)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(blockSize), blockSize, "blockSize must be a positive number");

            if (largeBufferMultiple <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(largeBufferMultiple), "largeBufferMultiple must be a positive number");

            if (maximumBufferSize < blockSize)
                throw new ArgumentOutOfRangeException(
                    nameof(maximumBufferSize), "maximumBufferSize must be at least blockSize");

            BlockSize = blockSize;
            LargeBufferMultiple = largeBufferMultiple;
            MaximumBufferSize = maximumBufferSize;
            UseExponentialLargeBuffer = useExponentialLargeBuffer;

            if (!IsLargeBufferSize(maximumBufferSize))
            {
                throw new ArgumentException(string.Format(
                    "maximumBufferSize is not {0} of largeBufferMultiple",
                    UseExponentialLargeBuffer ? "an exponential" : "a multiple"), nameof(maximumBufferSize));
            }

            _smallPool = new ConcurrentStack<byte[]>();
            int numLargePools = useExponentialLargeBuffer
                ? ((int)Math.Log(maximumBufferSize / largeBufferMultiple, 2) + 1)
                : (maximumBufferSize / largeBufferMultiple);

            // +1 to store size of bytes in use that are too large to be pooled
            _largeBufferInUseSize = new long[numLargePools + 1];
            _largeBufferFreeSize = new long[numLargePools];

            _largePools = new ConcurrentStack<byte[]>[numLargePools];

            for (int i = 0; i < _largePools.Length; ++i)
                _largePools[i] = new ConcurrentStack<byte[]>();

            Events.Writer.MemoryStreamManagerInitialized(blockSize, largeBufferMultiple, maximumBufferSize);
        }

        #endregion

        /// <summary>
        /// The size of each block. It must be set at creation and cannot be changed.
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// All buffers are multiples/exponentials of this number. It must be set at creation and cannot be changed.
        /// </summary>
        public int LargeBufferMultiple { get; }

        /// <summary>
        /// Use multiple large buffer allocation strategy. It must be set at creation and cannot be changed.
        /// </summary>
        public bool UseMultipleLargeBuffer => !UseExponentialLargeBuffer;

        /// <summary>
        /// Use exponential large buffer allocation strategy. It must be set at creation and cannot be changed.
        /// </summary>
        public bool UseExponentialLargeBuffer { get; }

        /// <summary>
        /// Gets the maximum buffer size.
        /// </summary>
        /// <remarks>Any buffer that is returned to the pool that is larger than this will be
        /// discarded and garbage collected.</remarks>
        public int MaximumBufferSize { get; }

        /// <summary>
        /// Number of bytes in small pool not currently in use
        /// </summary>
        public long SmallPoolFreeSize => _smallPoolFreeSize;

        /// <summary>
        /// Number of bytes currently in use by stream from the small pool
        /// </summary>
        public long SmallPoolInUseSize => _smallPoolInUseSize;

        /// <summary>
        /// Number of bytes in large pool not currently in use
        /// </summary>
        public long LargePoolFreeSize
        {
            get
            {
                long sum = 0;
                foreach (long freeSize in _largeBufferFreeSize)
                    sum += freeSize;

                return sum;
            }
        }

        /// <summary>
        /// Number of bytes currently in use by streams from the large pool
        /// </summary>
        public long LargePoolInUseSize
        {
            get
            {
                long sum = 0;
                foreach (long inUseSize in _largeBufferInUseSize)
                    sum += inUseSize;

                return sum;
            }
        }

        /// <summary>
        /// How many blocks are in the small pool
        /// </summary>
        public long SmallBlocksFree => _smallPool.Count;

        /// <summary>
        /// How many buffers are in the large pool
        /// </summary>
        public long LargeBuffersFree
        {
            get
            {
                long free = 0;
                foreach (var pool in _largePools)
                {
                    free += pool.Count;
                }
                return free;
            }
        }

        /// <summary>
        /// How many bytes of small free blocks to allow before we start dropping
        /// those returned to us.
        /// </summary>
        public long MaximumFreeSmallPoolBytes { get; set; }

        /// <summary>
        /// How many bytes of large free buffers to allow before we start dropping
        /// those returned to us.
        /// </summary>
        public long MaximumFreeLargePoolBytes { get; set; }

        /// <summary>
        /// Maximum stream capacity in bytes. Attempts to set a larger capacity will
        /// result in an exception.
        /// </summary>
        /// <remarks>A value of 0 indicates no limit.</remarks>
        public long MaximumStreamCapacity { get; set; }

        /// <summary>
        /// Whether to save callstacks for stream allocations. This can help in debugging.
        /// It should NEVER be turned on generally in production.
        /// </summary>
        public bool GenerateCallStacks { get; set; }

        /// <summary>
        /// Whether dirty buffers can be immediately returned to the buffer pool. E.g. when GetBuffer() is called on
        /// a stream and creates a single large buffer, if this setting is enabled, the other blocks will be returned
        /// to the buffer pool immediately.
        /// Note when enabling this setting that the user is responsible for ensuring that any buffer previously
        /// retrieved from a stream which is subsequently modified is not used after modification (as it may no longer
        /// be valid).
        /// </summary>
        public bool AggressiveBufferReturn { get; set; }

        /// <summary>
        /// Removes and returns a single block from the pool.
        /// </summary>
        /// <returns>A byte[] array</returns>
        public byte[] GetBlock()
        {
            if (!_smallPool.TryPop(out byte[] block))
            {
                // We'll add this back to the pool when the stream is disposed
                // (unless our free pool is too large)
                block = new byte[BlockSize];
                Events.Writer.MemoryStreamNewBlockCreated(_smallPoolInUseSize);
                ReportBlockCreated();
            }
            else
            {
                Interlocked.Add(ref _smallPoolFreeSize, -BlockSize);
            }

            Interlocked.Add(ref _smallPoolInUseSize, BlockSize);
            return block;
        }

        /// <summary>
        /// Returns a buffer of arbitrary size from the large buffer pool. This buffer
        /// will be at least the requiredSize and always be a multiple/exponential of largeBufferMultiple.
        /// </summary>
        /// <param name="requiredSize">The minimum length of the buffer</param>
        /// <param name="tag">The tag of the stream returning this buffer, for logging if necessary.</param>
        /// <returns>A buffer of at least the required size.</returns>
        public byte[] GetLargeBuffer(int requiredSize, string tag)
        {
            requiredSize = RoundToLargeBufferSize(requiredSize);

            byte[] buffer;
            int poolIndex = GetPoolIndex(requiredSize);
            if (poolIndex < _largePools.Length)
            {
                if (!_largePools[poolIndex].TryPop(out buffer))
                {
                    buffer = new byte[requiredSize];

                    Events.Writer.MemoryStreamNewLargeBufferCreated(requiredSize, LargePoolInUseSize);
                    ReportLargeBufferCreated();
                }
                else
                {
                    Interlocked.Add(ref _largeBufferFreeSize[poolIndex], -buffer.Length);
                }
            }
            else
            {
                // Buffer is too large to pool. They get a new buffer.

                // We still want to track the size, though, and we've reserved a slot
                // in the end of the inuse array for nonpooled bytes in use.
                poolIndex = _largeBufferInUseSize.Length - 1;

                // We still want to round up to reduce heap fragmentation.
                buffer = new byte[requiredSize];
                string callStack = null;
                if (GenerateCallStacks)
                {
                    // Grab the stack -- we want to know who requires such large buffers
                    callStack = Environment.StackTrace;
                }
                Events.Writer.MemoryStreamNonPooledLargeBufferCreated(requiredSize, tag, callStack);
                ReportLargeBufferCreated();
            }

            Interlocked.Add(ref _largeBufferInUseSize[poolIndex], buffer.Length);

            return buffer;
        }

        private int RoundToLargeBufferSize(int requiredSize)
        {
            if (UseExponentialLargeBuffer)
            {
                int pow = 1;
                while (LargeBufferMultiple * pow < requiredSize)
                    pow <<= 1;
                return LargeBufferMultiple * pow;
            }
            else
            {
                return ((requiredSize + LargeBufferMultiple - 1) / LargeBufferMultiple) * LargeBufferMultiple;
            }
        }

        private bool IsLargeBufferSize(int value)
        {
            return (value != 0) && (UseExponentialLargeBuffer
                ? (value == RoundToLargeBufferSize(value))
                : (value % LargeBufferMultiple) == 0);
        }

        private int GetPoolIndex(int length)
        {
            if (UseExponentialLargeBuffer)
            {
                int index = 0;
                while ((LargeBufferMultiple << index) < length)
                    ++index;
                return index;
            }
            else
            {
                return length / LargeBufferMultiple - 1;
            }
        }

        /// <summary>
        /// Returns the buffer to the large pool
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        /// <param name="tag">The tag of the stream returning this buffer, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentException">buffer.Length is not a multiple/exponential of LargeBufferMultiple (it did not originate from this pool)</exception>
        public void ReturnLargeBuffer(byte[] buffer, string tag)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (!IsLargeBufferSize(buffer.Length))
            {
                throw new ArgumentException(string.Format(
                    "buffer did not originate from this memory manager. The size is not {0} of ",
                    UseExponentialLargeBuffer ? "an exponential" : "a multiple") + LargeBufferMultiple);
            }

            int poolIndex = GetPoolIndex(buffer.Length);
            if (poolIndex < _largePools.Length)
            {
                if ((_largePools[poolIndex].Count + 1) * buffer.Length <= MaximumFreeLargePoolBytes ||
                    MaximumFreeLargePoolBytes == 0)
                {
                    _largePools[poolIndex].Push(buffer);
                    Interlocked.Add(ref _largeBufferFreeSize[poolIndex], buffer.Length);
                }
                else
                {
                    Events.Writer.MemoryStreamDiscardBuffer(
                        Events.MemoryStreamBufferType.Large, tag, Events.MemoryStreamDiscardReason.EnoughFree);
                    ReportLargeBufferDiscarded(Events.MemoryStreamDiscardReason.EnoughFree);
                }
            }
            else
            {
                // This is a non-poolable buffer, but we still want to track its size for inuse
                // analysis. We have space in the inuse array for this.
                poolIndex = _largeBufferInUseSize.Length - 1;

                Events.Writer.MemoryStreamDiscardBuffer(
                    Events.MemoryStreamBufferType.Large, tag, Events.MemoryStreamDiscardReason.TooLarge);
                ReportLargeBufferDiscarded(Events.MemoryStreamDiscardReason.TooLarge);
            }

            Interlocked.Add(ref _largeBufferInUseSize[poolIndex], -buffer.Length);

            ReportUsageReport(
                _smallPoolInUseSize, _smallPoolFreeSize, LargePoolInUseSize, LargePoolFreeSize);
        }

        /// <summary>
        /// Returns a block to the pool.
        /// </summary>
        /// <param name="blocks">Collection of blocks to return to the pool</param>
        /// <param name="tag">The tag of the stream returning these blocks, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">blocks is null</exception>
        /// <exception cref="ArgumentException">blocks contains buffers that are the wrong size (or null) for this memory manager</exception>
        public void ReturnBlock(byte[] block, string tag = null)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (block.Length != BlockSize)
                throw new ArgumentException("block is not BlockSize in length");

            Interlocked.Add(ref _smallPoolInUseSize, -BlockSize);

            ReturnBlockCore(block, tag);
        }

        /// <summary>
        /// Returns the blocks to the pool
        /// </summary>
        /// <param name="blocks">Collection of blocks to return to the pool</param>
        /// <param name="tag">The tag of the stream returning these blocks, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">blocks is null</exception>
        /// <exception cref="ArgumentException">blocks contains buffers that are the wrong size (or null) for this memory manager</exception>
        public void ReturnBlocks(IList<byte[]> blocks, string tag)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block == null || block.Length != BlockSize)
                    throw new ArgumentException("blocks contains buffers that are not BlockSize in length");
            }

            int bytesToReturn = blocks.Count * BlockSize;
            Interlocked.Add(ref _smallPoolInUseSize, -bytesToReturn);

            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (!ReturnBlockCore(block, tag))
                    break;
            }

            ReportUsageReport(
                _smallPoolInUseSize, _smallPoolFreeSize, LargePoolInUseSize, LargePoolFreeSize);
        }

        private bool ReturnBlockCore(byte[] block, string tag = null)
        {
            if (MaximumFreeSmallPoolBytes == 0 || SmallPoolFreeSize < MaximumFreeSmallPoolBytes)
            {
                Interlocked.Add(ref _smallPoolFreeSize, BlockSize);
                _smallPool.Push(block);
                return true;
            }
            else
            {
                Events.Writer.MemoryStreamDiscardBuffer(
                    Events.MemoryStreamBufferType.Small, tag, Events.MemoryStreamDiscardReason.EnoughFree);
                ReportBlockDiscarded();
                return false;
            }
        }

        internal void ReportBlockCreated() => BlockCreated?.Invoke();

        internal void ReportBlockDiscarded() => BlockDiscarded?.Invoke();

        internal void ReportLargeBufferCreated() => LargeBufferCreated?.Invoke();

        internal void ReportLargeBufferDiscarded(Events.MemoryStreamDiscardReason reason)
        {
            LargeBufferDiscarded?.Invoke(reason);
        }

        internal void ReportStreamCreated() => StreamCreated?.Invoke();

        internal void ReportStreamDisposed() => StreamDisposed?.Invoke();

        internal void ReportStreamFinalized() => StreamFinalized?.Invoke();

        internal void ReportStreamLength(long bytes) => StreamLength?.Invoke(bytes);

        internal void ReportStreamToArray() => StreamConvertedToArray?.Invoke();

        internal void ReportUsageReport(
            long smallPoolInUseBytes, long smallPoolFreeBytes, long largePoolInUseBytes, long largePoolFreeBytes)
        {
            UsageReport?.Invoke(smallPoolInUseBytes, smallPoolFreeBytes, largePoolInUseBytes, largePoolFreeBytes);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with no tag and a default initial capacity.
        /// </summary>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream() => new RecyclableMemoryStream(this);

        /// <summary>
        /// Retrieve a new MemoryStream object with no tag and a default initial capacity.
        /// </summary>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(Guid id) => new RecyclableMemoryStream(this, id);

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and a default initial capacity.
        /// </summary>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(string tag) => new RecyclableMemoryStream(this, tag);

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and a default initial capacity.
        /// </summary>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public MemoryStream GetStream(Guid id, string tag) => new RecyclableMemoryStream(this, id, tag);

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity.
        /// </summary>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(string tag, int requiredSize)
        {
            return new RecyclableMemoryStream(this, tag, requiredSize);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity.
        /// </summary>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(int requiredSize) => GetStream(null, requiredSize);

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity.
        /// </summary>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(Guid id, string tag, int requiredSize)
        {
            return new RecyclableMemoryStream(this, id, tag, requiredSize);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity, possibly using
        /// a single contiguous underlying buffer.
        /// </summary>
        /// <remarks>Retrieving a MemoryStream which provides a single contiguous buffer can be useful in situations
        /// where the initial size is known and it is desirable to avoid copying data between the smaller underlying
        /// buffers to a single large one. This is most helpful when you know that you will always call GetBuffer
        /// on the underlying stream.</remarks>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <param name="asContiguousBuffer">Whether to attempt to use a single contiguous buffer.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(Guid id, string tag, int requiredSize, bool asContiguousBuffer)
        {
            if (!asContiguousBuffer || requiredSize <= BlockSize)
                return GetStream(id, tag, requiredSize);
            
            return new RecyclableMemoryStream(this, id, tag, requiredSize, GetLargeBuffer(requiredSize, tag));
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity, possibly using
        /// a single contiguous underlying buffer.
        /// </summary>
        /// <remarks>Retrieving a MemoryStream which provides a single contiguous buffer can be useful in situations
        /// where the initial size is known and it is desirable to avoid copying data between the smaller underlying
        /// buffers to a single large one. This is most helpful when you know that you will always call GetBuffer
        /// on the underlying stream.</remarks>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <param name="asContiguousBuffer">Whether to attempt to use a single contiguous buffer.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(string tag, int requiredSize, bool asContiguousBuffer)
        {
            return GetStream(Guid.NewGuid(), tag, requiredSize, asContiguousBuffer);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and with contents copied from the provided
        /// buffer. The provided buffer is not wrapped or used after construction.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="buffer">The byte buffer to copy data from.</param>
        /// <param name="offset">The offset from the start of the buffer to copy from.</param>
        /// <param name="count">The number of bytes to copy from the buffer.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(Guid id, string tag, byte[] buffer, int offset, int count)
        {
            RecyclableMemoryStream stream = null;
            try
            {
                stream = new RecyclableMemoryStream(this, id, tag, count);
                stream.Write(buffer, offset, count);
                stream.Position = 0;
                return stream;
            }
            catch
            {
                stream?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the contents copied from the provided
        /// buffer. The provided buffer is not wrapped or used after construction.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="buffer">The byte buffer to copy data from.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(byte[] buffer)
        {
            return GetStream(null, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and with contents copied from the provided
        /// buffer. The provided buffer is not wrapped or used after construction.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="buffer">The byte buffer to copy data from.</param>
        /// <param name="offset">The offset from the start of the buffer to copy from.</param>
        /// <param name="count">The number of bytes to copy from the buffer.</param>
        /// <returns>A MemoryStream.</returns>
        public RecyclableMemoryStream GetStream(string tag, byte[] buffer, int offset, int count)
        {
            return GetStream(Guid.NewGuid(), tag, buffer, offset, count);
        }

        /// <summary>
        /// Triggered when a new block is created.
        /// </summary>
        public event EventHandler BlockCreated;

        /// <summary>
        /// Triggered when a new block is created.
        /// </summary>
        public event EventHandler BlockDiscarded;

        /// <summary>
        /// Triggered when a new large buffer is created.
        /// </summary>
        public event EventHandler LargeBufferCreated;

        /// <summary>
        /// Triggered when a new stream is created.
        /// </summary>
        public event EventHandler StreamCreated;

        /// <summary>
        /// Triggered when a stream is disposed.
        /// </summary>
        public event EventHandler StreamDisposed;

        /// <summary>
        /// Triggered when a stream is finalized.
        /// </summary>
        public event EventHandler StreamFinalized;

        /// <summary>
        /// Triggered when a stream is finalized.
        /// </summary>
        public event StreamLengthReportHandler StreamLength;

        /// <summary>
        /// Triggered when a user converts a stream to array.
        /// </summary>
        public event EventHandler StreamConvertedToArray;

        /// <summary>
        /// Triggered when a large buffer is discarded, along with the reason for the discard.
        /// </summary>
        public event LargeBufferDiscardedEventHandler LargeBufferDiscarded;

        /// <summary>
        /// Periodically triggered to report usage statistics.
        /// </summary>
        public event UsageReportEventHandler UsageReport;
    }
}
