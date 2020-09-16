using System;

namespace MCServerSharp.Components
{
    /// <summary>
    /// Represents a behavior that acts upon an entity.
    /// </summary>
    public abstract class Component
    {
        public ComponentEntity Entity { get; }

        protected Component(ComponentEntity entity)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public abstract void Tick();
    }
}
