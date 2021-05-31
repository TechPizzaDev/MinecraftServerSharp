using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public class DirectBlockPalette : IBlockPalette
    {
        private BlockState[] _blockStates;
        private Dictionary<Identifier, BlockDescription> _blockLookupUtf16;
        private Dictionary<Utf8Identifier, BlockDescription> _blockLookup;

        public BlockDescription this[Identifier identifier] => _blockLookupUtf16[identifier];
        public BlockDescription this[string identifier] => _blockLookupUtf16[new Identifier(identifier)];
        public BlockDescription this[Utf8Identifier identifier] => _blockLookup[identifier];
        public BlockDescription this[Utf8Memory identifier] => _blockLookup[Utf8Identifier.CreateUnsafe(identifier)];

        public int BitsPerBlock { get; }
        public int Count => _blockStates.Length;

        public DirectBlockPalette(IEnumerable<BlockDescription> blocks)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            _blockLookupUtf16 = new Dictionary<Identifier, BlockDescription>();
            _blockLookup = new Dictionary<Utf8Identifier, BlockDescription>();

            var stateLookup = new Dictionary<uint, BlockState>();
            int stateCount = 0;
            uint maxStateId = 0;

            foreach (BlockDescription block in blocks)
            {
                _blockLookupUtf16.Add(block.IdentifierUtf16, block);
                _blockLookup.Add(block.Identifier, block);

                stateCount += block.StateCount;
                foreach (BlockState state in block.States.Span)
                {
                    stateLookup.Add(state.StateId, state);
                    maxStateId = Math.Max(maxStateId, state.StateId);
                }
            }

            uint maxStateCount = maxStateId + 1;
            if (maxStateCount != stateCount)
            {
                throw new ArgumentException(
                    "The amount of block states does not match the highest state ID.", nameof(blocks));
            }

            _blockStates = new BlockState[maxStateCount];
            for (uint stateId = 0; stateId < _blockStates.Length; stateId++)
            {
                if (!stateLookup.TryGetValue(stateId, out var state))
                    throw new Exception("Missing state with Id " + stateId);
                _blockStates[stateId] = state;
            }
            BitsPerBlock = (int)Math.Ceiling(Math.Log2(Count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint IdForBlock(BlockState state)
        {
            return state.StateId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockState BlockForId(uint id)
        {
            return _blockStates[id];
        }

        public void Write(NetBinaryWriter writer)
        {
        }

        public int GetEncodedSize()
        {
            return 0;
        }
    }
}
