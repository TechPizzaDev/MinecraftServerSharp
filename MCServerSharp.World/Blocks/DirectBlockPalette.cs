using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MCServerSharp.Data.IO;

namespace MCServerSharp.Blocks
{
    public class DirectBlockPalette : IBlockPalette
    {
        private BlockState[] _blockStates;
        private Dictionary<Identifier, BlockDescription> _blockLookup;
        private Dictionary<Utf8Identifier, BlockDescription> _utf8BlockLookup;

        public BlockDescription this[Identifier identifier] => _blockLookup[identifier];
        public BlockDescription this[string identifier] => _blockLookup[new Identifier(identifier)];
        public BlockDescription this[Utf8Identifier identifier] => _utf8BlockLookup[identifier];
        public BlockDescription this[Utf8String identifier] => this[new Utf8Identifier(identifier)];

        public int BitsPerBlock { get; }
        public int Count => _blockStates.Length;

        // TODO: remove this field

        public DirectBlockPalette(IEnumerable<BlockDescription> blocks)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            _blockLookup = new Dictionary<Identifier, BlockDescription>();
            _utf8BlockLookup = new Dictionary<Utf8Identifier, BlockDescription>();

            // TODO: optimize/dont use an intermediate dictionary?
            var stateLookup = new Dictionary<uint, BlockState>();
            int stateCount = 0;
            uint maxStateId = 0;

            foreach (BlockDescription block in blocks)
            {
                _blockLookup.Add(block.Identifier, block);
                _utf8BlockLookup.Add(block.Identifier.ToUtf8Identifier(), block);

                stateCount += block.StateCount;
                foreach (BlockState state in block.States.Span)
                {
                    stateLookup.Add(state.StateId, state);
                    maxStateId = Math.Max(maxStateId, state.StateId);
                }
            }

            _blockStates = new BlockState[maxStateId + 1];
            for (uint stateId = 0; stateId < _blockStates.Length; stateId++)
            {
                if (!stateLookup.TryGetValue(stateId, out var state))
                    throw new Exception("Missing state for Id " + stateId);
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
