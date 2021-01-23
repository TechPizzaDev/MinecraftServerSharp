using System;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Components;

namespace MCServerSharp.Entities.Mobs
{
    public static class PlayerComponentExtensions
    {
        public static Player GetPlayer(this ComponentCollection components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            return components.Get<Component<Player>>().Entity;
        }

        public static Player GetPlayer(this IComponentEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return entity.Components.Get<Component<Player>>().Entity;
        }

        public static Player GetPlayer(this Component hostComponent)
        {
            if (hostComponent == null)
                throw new ArgumentNullException(nameof(hostComponent));

            return GetPlayer(hostComponent.Entity);
        }

        public static bool GetPlayer(
            this ComponentCollection components, [NotNullWhen(true)] out Player? player)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            if (components.Get<Component<Player>>(out var component))
            {
                player = component.Entity;
                return true;
            }
            player = null;
            return false;
        }

        public static bool GetPlayer(
            this IComponentEntity entity, [NotNullWhen(true)] out Player? player)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Components.Get<Component<Player>>(out var component))
            {
                player = component.Entity;
                return true;
            }
            player = null;
            return false;
        }

        public static bool GetPlayer(
            this Component hostComponent, [NotNullWhen(true)] out Player? player)
        {
            if (hostComponent == null)
                throw new ArgumentNullException(nameof(hostComponent));

            return GetPlayer(hostComponent.Entity, out player);
        }
    }
}
