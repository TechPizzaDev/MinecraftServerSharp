using System;

namespace MCServerSharp.Components
{
    /// <summary>
    /// Represents a behavior that acts upon an entity.
    /// </summary>
    public abstract class Component
    {
        public IComponentEntity Entity { get; }

        protected Component(IComponentEntity entity)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <summary>
    /// Represents a behavior that acts upon an entity.
    /// </summary>
    public abstract class Component<TEntity> : Component
        where TEntity : IComponentEntity
    {
        public new TEntity Entity => (TEntity)base.Entity;

        protected Component(TEntity entity) : base(entity)
        {
        }
    }
}
