using System;
using System.Buffers;
using System.Collections.Generic;
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

        private IndirectBlockPalette()
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

        public void Write(NetBinaryWriter writer)
        {
            // Palette Length
            writer.WriteVar(Count);

            // Palette
            for (uint id = 0; id < _idToBlock.Length; id++)
            {
                BlockState state = _idToBlock[id];
                writer.WriteVar(state.Id);
            }
        }

        public int GetEncodedSize()
        {
            throw new NotImplementedException();
        }

        public static OperationStatus Read(
            NetBinaryReader reader, DirectBlockPalette globalPalette, out IndirectBlockPalette? blockPalette)
        {
            if (globalPalette == null)
                throw new ArgumentNullException(nameof(globalPalette));

            blockPalette = null;

            OperationStatus status;
            if ((status = reader.Read(out VarInt length)) != OperationStatus.Done)
                return status;

            blockPalette = new IndirectBlockPalette();
            for (uint id = 0; id < length; id++)
            {
                if ((status = reader.Read(out VarInt stateId)) != OperationStatus.Done)
                    return status;

                uint uStateId = (uint)stateId.Value;
                BlockState state = globalPalette.BlockForId(uStateId);

                blockPalette._idToBlock[id] = state;
                blockPalette._blockToId.Add(state, id);
            }
            blockPalette.BitsPerBlock = (int)Math.Ceiling(Math.Log2(blockPalette.Count));

            return OperationStatus.Done;
        }
    }
}
