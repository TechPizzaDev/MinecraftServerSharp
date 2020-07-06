using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MinecraftServerSharp.NBT
{
    public sealed partial class NbtDocument
    {
        private struct RowFrameStack : IDisposable
        {
            private byte[] _rentedBuffer;
            private int _topOfStack;

            internal RowFrameStack(int initialSize)
            {
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialSize);
                _topOfStack = _rentedBuffer.Length;
            }

            public void Dispose()
            {
                byte[] toReturn = _rentedBuffer;
                _rentedBuffer = null!;
                _topOfStack = 0;

                if (toReturn != null)
                {
                    // The data in this rented buffer only conveys the positions and
                    // lengths of tokens in a document, but no content; so it does not
                    // need to be cleared.
                    ArrayPool<byte>.Shared.Return(toReturn);
                }
            }

            internal void Push(RowFrame row)
            {
                if (_topOfStack < RowFrame.Size)
                {
                    Enlarge();
                }

                _topOfStack -= RowFrame.Size;
                MemoryMarshal.Write(_rentedBuffer.AsSpan(_topOfStack), ref row);
            }

            internal RowFrame Pop()
            {
                Debug.Assert(_topOfStack <= _rentedBuffer.Length - RowFrame.Size);
                RowFrame row = MemoryMarshal.Read<RowFrame>(_rentedBuffer.AsSpan(_topOfStack));
                _topOfStack += RowFrame.Size;
                return row;
            }

            private void Enlarge()
            {
                byte[] toReturn = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);

                Buffer.BlockCopy(
                    toReturn,
                    _topOfStack,
                    _rentedBuffer,
                    _rentedBuffer.Length - toReturn.Length + _topOfStack,
                    toReturn.Length - _topOfStack);

                _topOfStack += _rentedBuffer.Length - toReturn.Length;

                // The data in this rented buffer only conveys the positions and
                // lengths of tokens in a document, but no content; so it does not
                // need to be cleared.
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
