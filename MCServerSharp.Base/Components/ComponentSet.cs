using System.Collections;
using System.Collections.Generic;

namespace MCServerSharp.Components
{
    /// <summary>
    /// Represents a set of components.
    /// </summary>
    public class ComponentSet : IReadOnlyCollection<Component>
    {
        private List<Component> _components;
        
        public int Count => _components.Count;

        public ComponentSet()
        {
            _components = new List<Component>();
        }

        public void Tick()
        {
            //var components = GetComponentSpan();
            //for (int i = 0; i < components.Length; i++)
            //    components[i].Tick();

            for (int i = 0; i < _components.Count; i++)
                _components[i].Tick();
        }

        // TODO: NET5
        //public ReadOnlySpan<Component> GetComponentSpan()
        //{
        //    return CollectionsMarshal.AsSpan(_components);
        //}

        public List<Component>.Enumerator GetEnumerator()
        {
            return _components.GetEnumerator();
        }

        IEnumerator<Component> IEnumerable<Component>.GetEnumerator()
        {
            return _components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _components.GetEnumerator();
        }
    }
}
