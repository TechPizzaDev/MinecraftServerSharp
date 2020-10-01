using System;
using MCServerSharp.Collections;
using static MCServerSharp.NBT.NbtReader;

namespace MCServerSharp.NBT
{
    public struct NbtReaderState : IDisposable
    {
        internal ByteStack<ContainerFrame> _containerInfoStack;

        public NbtOptions Options { get; }

        public NbtReaderState(NbtOptions? options = null) : this()
        {
            Options = options ?? NbtOptions.JavaDefault;

            _containerInfoStack = new ByteStack<ContainerFrame>(NbtOptions.DefaultMaxDepth, clearOnReturn: false);
        }

        public void Dispose()
        {
            _containerInfoStack.Dispose();
        }
    }
}
