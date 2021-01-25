using MCServerSharp.Collections;
using static MCServerSharp.NBT.NbtReader;

namespace MCServerSharp.NBT
{
    public ref struct NbtReaderState
    {
        internal ByteStack<ContainerFrame> _containerInfoStack;

        public NbtOptions Options { get; }

        public NbtReaderState(ByteStack<ContainerFrame> stack, NbtOptions? options = null) : this()
        {
            Options = options ?? NbtOptions.JavaDefault;

            _containerInfoStack = stack;
        }

        public void Dispose()
        {
            _containerInfoStack.Dispose();
        }
    }
}
