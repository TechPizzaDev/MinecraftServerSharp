using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MCServerSharp.NBT
{
    public readonly partial struct NbtElement
    {
        /// <summary>
        /// An enumerable and enumerator for the contents of an NBT container.
        /// </summary>
        [DebuggerDisplay("{Current,nq}")]
        public struct ContainerEnumerator : IEnumerable<NbtElement>, IEnumerator<NbtElement>
        {
            private readonly NbtElement _container;
            private readonly int _targetEndIndex;
            private int _currentIndex;

            public NbtElement Current
            {
                get
                {
                    if (_currentIndex < 0)
                        return default;
                    return new NbtElement(_container._parent, _currentIndex);
                }
            }

            object IEnumerator.Current => Current;

            internal ContainerEnumerator(NbtElement target)
            {
                _container = target;
                _targetEndIndex = target._parent.GetEndIndex(_container._index, false);
                _currentIndex = -1;
            }

            /// <summary>
            /// Returns an enumerator that iterates through a container.
            /// </summary>
            /// <returns>
            /// A <see cref="ContainerEnumerator"/> that can be used to iterate through the container.
            /// </returns>
            public ContainerEnumerator GetEnumerator()
            {
                ContainerEnumerator ator = this;
                ator._currentIndex = -1;
                return ator;
            }

            IEnumerator<NbtElement> IEnumerable<NbtElement>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Dispose()
            {
                _currentIndex = _targetEndIndex;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            public bool MoveNext()
            {
                if (_currentIndex >= _targetEndIndex)
                    return false;

                if (_currentIndex < 0)
                    _currentIndex = _container._index + NbtDocument.DbRow.Size;
                else
                    _currentIndex = _container._parent.GetEndIndex(_currentIndex, true);

                return _currentIndex < _targetEndIndex;
            }
        }
    }
}
