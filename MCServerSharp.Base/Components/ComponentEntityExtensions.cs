using System;
using System.Diagnostics.CodeAnalysis;

namespace MCServerSharp.Components
{
    public static class ComponentComponentExtensions
    {
        public static TComponent GetComponent<TComponent>(this IComponentEntity entity)
            where TComponent : Component
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return entity.Components.Get<TComponent>();
        }

        public static bool GetComponent<TComponent>(
            this IComponentEntity entity, [NotNullWhen(true)] out TComponent? component)
            where TComponent : Component
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return entity.Components.Get(out component);
        }

        public static TComponent GetComponent<TComponent>(this Component hostComponent)
            where TComponent : Component
        {
            if (hostComponent == null)
                throw new ArgumentNullException(nameof(hostComponent));

            return GetComponent<TComponent>(hostComponent.Entity);
        }

        public static bool GetComponent<TComponent>(
            this Component hostComponent, [NotNullWhen(true)] out TComponent? component)
            where TComponent : Component
        {
            if (hostComponent == null)
                throw new ArgumentNullException(nameof(hostComponent));

            return GetComponent(hostComponent.Entity, out component);
        }
    }
}
