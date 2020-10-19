using System;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Components;

namespace MCServerSharp.Entity.Mob
{
    public static class PlayerComponentExtensions
    {
        public static Player GetPlayer(this ComponentCollection components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            return components.Get<PlayerComponent>().Entity;
        }

        public static Player GetPlayer(this ComponentEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return entity.Components.Get<PlayerComponent>().Entity;
        }

        public static bool GetPlayer(this ComponentCollection components, [NotNullWhen(true)] out Player? player)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            if (components.Get<PlayerComponent>(out var component))
            {
                player = component.Entity;
                return true;
            }
            player = null;
            return false;
        }

        public static bool GetPlayer(this ComponentEntity entity, [NotNullWhen(true)] out Player? player)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Components.Get<PlayerComponent>(out var component))
            {
                player = component.Entity;
                return true;
            }
            player = null;
            return false;
        }
    }
}
