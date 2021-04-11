using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public class IndirectBlockPalette : IBlockPalette
    {
        private BlockState[] _idToBlock;
        private Dictionary<BlockState, uint> _blockToId;

        public int BitsPerBlock { get; private set; }
        public int Count => _idToBlock.Length;

        protected IndirectBlockPalette(int capacity)
        {
            _idToBlock = new BlockState[capacity];
            _blockToId = new Dictionary<BlockState, uint>(capacity);
        }

        protected IndirectBlockPalette()
        {
            _idToBlock = Array.Empty<BlockState>();
            _blockToId = new Dictionary<BlockState, uint>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockState BlockForId(uint id)
        {
            return _idToBlock[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint IdForBlock(BlockState state)
        {
            return _blockToId[state];
        }

        [SkipLocalsInit]
        public void Write(NetBinaryWriter writer)
        {
            Span<uint> tmp = stackalloc uint[512];
            int offset = 0;

            // Palette Length
            tmp[offset++] = (uint)Count;

            // Palette
            ReadOnlySpan<BlockState> idToBlock = _idToBlock;
            do
            {
                ReadOnlySpan<BlockState> blockSlice = idToBlock;

                int available = tmp.Length - offset;
                if (blockSlice.Length > available)
                    blockSlice = blockSlice.Slice(0, available);

                for (int i = 0; i < blockSlice.Length; i++)
                {
                    BlockState state = blockSlice[i];
                    tmp[offset++] = state.StateId;
                }

                writer.WriteVar(tmp.Slice(0, offset));
                offset = 0;

                idToBlock = idToBlock[blockSlice.Length..];
            }
            while (idToBlock.Length > 0);
        }

        public int GetEncodedSize()
        {
            int size = 0;

            // Palette Length
            size += VarInt.GetEncodedSize(Count);

            // Palette
            for (uint id = 0; id < _idToBlock.Length; id++)
            {
                BlockState state = _idToBlock[id];
                size += VarInt.GetEncodedSize(state.StateId);
            }

            return size;
        }

        private void UpdateBitsPerBlock()
        {
            BitsPerBlock = (int)Math.Max(1, Math.Ceiling(Math.Log2(Count)));
        }

        public static IndirectBlockPalette CreateUnsafe(BlockState[] blocks)
        {
            IndirectBlockPalette palette = new(blocks.Length);
            palette._idToBlock = blocks;
            for (uint id = 0; id < blocks.Length; id++)
            {
                palette._blockToId.Add(blocks[id], id);
            }

            palette.UpdateBitsPerBlock();
            return palette;
        }

        public static IndirectBlockPalette Create(IEnumerable<BlockState> blocks)
        {
            BlockState[] blockArray = blocks.ToArray();
            return CreateUnsafe(blockArray);
        }

        public static OperationStatus Read(
            NetBinaryReader reader, DirectBlockPalette globalPalette, out IndirectBlockPalette? indirectPalette)
        {
            if (globalPalette == null)
                throw new ArgumentNullException(nameof(globalPalette));

            indirectPalette = null;

            OperationStatus status;
            if ((status = reader.Read(out VarInt length)) != OperationStatus.Done)
                return status;

            indirectPalette = new IndirectBlockPalette(length);
            for (uint id = 0; id < length; id++)
            {
                if ((status = reader.Read(out VarInt stateId)) != OperationStatus.Done)
                    return status;

                uint uStateId = (uint)stateId.Value;
                BlockState state = globalPalette.BlockForId(uStateId);

                indirectPalette._idToBlock[id] = state;
                indirectPalette._blockToId.Add(state, id);
            }

            indirectPalette.UpdateBitsPerBlock();
            return OperationStatus.Done;
        }
    }
}
