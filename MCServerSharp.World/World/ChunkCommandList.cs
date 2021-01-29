using System;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public abstract class ChunkCommandList
    {
        protected bool IsReady { get; private set; }

        public IChunk Chunk { get; }

        public ChunkCommandList(IChunk chunk)
        {
            Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        }

        /// <summary>
        /// Throws if the command list is not ready.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Begin"/> was not called or <see cref="End"/> has already been called.
        /// </exception>
        protected void AssertReady()
        {
            if (!IsReady)
                throw new InvalidOperationException("The command list was not ready.");
        }

        /// <summary>
        /// Puts the command list into a state where commands can be issued.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Begin"/> has already been called or <see cref="End"/> was not called.
        /// </exception>
        public virtual void Begin()
        {
            if (IsReady)
                throw new InvalidOperationException("The command list was already ready.");
            IsReady = true;
        }

        /// <summary>
        /// Completes this command list,
        /// putting it into an executable state for <see cref="IChunk.SubmitCommands(ChunkCommandList)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Begin"/> was not called or <see cref="End"/> has already been called.
        /// </exception>
        public virtual void End()
        {
            AssertReady();
            IsReady = false;
        }

        /// <summary>
        /// Requests blocks from the chunk.
        /// </summary>
        /// <param name="indices">The indices mapping <paramref name="source"/> to the chunk.</param>
        /// <param name="destination">The destination that gets filled upon <see cref="IChunk.SubmitCommands(ChunkCommandList)"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Begin"/> was not called or <see cref="End"/> has already been called.
        /// </exception>
        public abstract void GetBlocks(ReadOnlySpan<BlockPosition> indices, Memory<BlockState> destination);

        /// <summary>
        /// Sets blocks on the chunk.
        /// </summary>
        /// <param name="indices">The indices mapping <paramref name="source"/> to the chunk.</param>
        /// <param name="source">The source that is used when setting blocks upon <see cref="IChunk.SubmitCommands(ChunkCommandList)"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Begin"/> was not called or <see cref="End"/> has already been called.
        /// </exception>
        public abstract void SetBlocks(ReadOnlySpan<BlockPosition> indices, ReadOnlyMemory<BlockState> source);
    }
}
