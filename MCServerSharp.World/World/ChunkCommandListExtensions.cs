using System;
using MCServerSharp.Blocks;
using MCServerSharp.Maths;

namespace MCServerSharp.World
{
    public static class ChunkCommandListExtensions
    {
        public static BlockState[] GetBlock(this ChunkCommandList commandList, BlockPosition position)
        {
            if (commandList == null)
                throw new ArgumentNullException(nameof(commandList));

            BlockState[] destination = new BlockState[1];
            commandList.GetBlocks(stackalloc BlockPosition[] { position }, destination);
            return destination;
        }

        public static void SetBlock(this ChunkCommandList commandList, BlockPosition position, BlockState block)
        {
            if (commandList == null)
                throw new ArgumentNullException(nameof(commandList));

            BlockState[] source = new BlockState[1];
            source[0] = block;
            commandList.SetBlocks(stackalloc BlockPosition[] { position }, source);
        }
    }
}
