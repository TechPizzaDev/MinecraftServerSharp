using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace MCServerSharp.Components
{
    public delegate TComponent GetOrAddFactory<TState, TComponent>([AllowNull] TState state)
        where TComponent : Component;

    /// <summary>
    /// Represents a collection of components.
    /// </summary>
    public class ComponentCollection : IReadOnlyCollection<Component>, ITickable
    {
        private static List<Component> _emptyComponents = new();
        private static List<ITickable> _emptyTickables = new();

        // TODO: consider swapping between list and hashset if 
        // a component amount threshold is reached

        private List<Component> _components;
        private List<ITickable> _tickables;

        public int Count => _components.Count;

        public ComponentCollection()
        {
            _components = _emptyComponents;
            _tickables = _emptyTickables;
        }

        public TComponent Get<TComponent>()
            where TComponent : Component
        {
            foreach (var component in _components)
            {
                if (component is TComponent genericComponent)
                    return genericComponent;
            }
            throw new KeyNotFoundException(typeof(TComponent).FullName);
        }

        public bool Get<TComponent>([NotNullWhen(true)] out TComponent? component)
            where TComponent : Component
        {
            foreach (var c in _components)
            {
                if (c is TComponent genericComponent)
                {
                    component = genericComponent;
                    return true;
                }
            }
            component = null;
            return false;
        }

        public TComponent GetOrAdd<TComponent>()
            where TComponent : Component, new()
        {
            foreach (var c in _components)
            {
                if (c is TComponent genericComponent)
                    return genericComponent;
            }
            return Add<TComponent>();
        }

        public TComponent GetOrAdd<TComponent>(Func<TComponent> factory)
            where TComponent : Component
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            foreach (var c in _components)
            {
                if (c is TComponent genericComponent)
                    return genericComponent;
            }

            var component = factory.Invoke();
            if (component == null)
                throw new InvalidOperationException("The factory returned null.");

            return Add(component);
        }

        public TComponent GetOrAdd<TState, TComponent>(
            [AllowNull] TState state,
            GetOrAddFactory<TState, TComponent> factory)
            where TComponent : Component
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            foreach (var c in _components)
            {
                if (c is TComponent genericComponent)
                    return genericComponent;
            }

            var component = factory.Invoke(state);
            if (component == null)
                throw new InvalidOperationException("The factory returned null.");

            return Add(component);
        }

        public bool Has<TComponent>()
            where TComponent : Component
        {
            foreach (var c in _components)
            {
                if (c is TComponent)
                    return true;
            }
            return false;
        }

        public TComponent Add<TComponent>(TComponent component)
            where TComponent : Component
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (Has<TComponent>())
                throw new InvalidOperationException();

            if (component is ITickable tickable)
            {
                if (_tickables == _emptyTickables)
                    _tickables = new();
                _tickables.Add(tickable);
            }

            if (_components == _emptyComponents)
                _components = new();
            _components.Add(component);

            return component;
        }

        public TComponent Add<TComponent>()
            where TComponent : Component, new()
        {
            if (Has<TComponent>())
                throw new InvalidOperationException();

            return Add(new TComponent());
        }

        public void Tick()
        {
            var tickables = GetTickableSpan();
            for (int i = 0; i < tickables.Length; i++)
                tickables[i].Tick();
        }

        public ReadOnlySpan<Component> GetComponentSpan()
        {
            return CollectionsMarshal.AsSpan(_components);
        }

        public ReadOnlySpan<ITickable> GetTickableSpan()
        {
            return CollectionsMarshal.AsSpan(_tickables);
        }

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
