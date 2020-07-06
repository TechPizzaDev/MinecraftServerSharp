using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinecraftServerSharp.NBT
{
    public readonly partial struct NbtElement
    {
        /// <summary>
        /// An enumerable and enumerator for the contents of an NBT container.
        /// </summary>
        [DebuggerDisplay("{Current,nq}")]
        public struct ContainerEnumerator : IEnumerable<NbtElement>, IEnumerator<NbtElement>
        {
            private readonly NbtElement _target;
            private readonly int _endIndexOrVersion;
            private int _currentIndex;

            internal ContainerEnumerator(NbtElement target)
            {
                _target = target;
                _endIndexOrVersion = target._parent.GetEndIndex(_target._index);
                _currentIndex = -1;
            }

            public NbtElement Current
            {
                get
                {
                    if (_currentIndex < 0)
                        return default;

                    return new NbtElement(_target._parent, _currentIndex);
                }
            }

            object IEnumerator.Current => Current;

            /// <summary>
            ///   Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            ///   A <see cref="ContainerEnumerator"/> value that can be used to iterate
            ///   through the array.
            /// </returns>
            public ContainerEnumerator GetEnumerator()
            {
                ContainerEnumerator ator = this;
                ator._currentIndex = -1;
                return ator;
            }

            IEnumerator<NbtElement> IEnumerable<NbtElement>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Dispose()
            {
                _currentIndex = _endIndexOrVersion;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            public bool MoveNext()
            {
                if (_currentIndex >= _endIndexOrVersion)
                    return false;

                if (_currentIndex < 0)
                    _currentIndex = _target._index + NbtDocument.DbRow.Size;
                else
                    _currentIndex = _target._parent.GetEndIndex(_currentIndex);

                return _currentIndex < _endIndexOrVersion;
            }
        }
    }
}
