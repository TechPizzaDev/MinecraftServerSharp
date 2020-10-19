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

    /// <summary>
    /// Represents a behavior that acts upon a component entity.
    /// </summary>
    public abstract class Component<TEntity> : Component
        where TEntity : ComponentEntity
    {
        public new TEntity Entity => (TEntity)base.Entity;

        protected Component(TEntity entity) : base(entity)
        {
        }
    }
}
