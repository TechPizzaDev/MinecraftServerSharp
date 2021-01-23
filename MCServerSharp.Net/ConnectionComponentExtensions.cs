using System;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Components;

namespace MCServerSharp.Net
{
    public static class PlayerComponentExtensions
    {
        public static NetConnection GetConnection(this ComponentCollection components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            return components.Get<Component<NetConnection>>().Entity;
        }

        public static NetConnection GetConnection(this IComponentEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return entity.Components.Get<Component<NetConnection>>().Entity;
        }

        public static bool GetConnection(
            this ComponentCollection components, [NotNullWhen(true)] out NetConnection? connection)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            if (components.Get<Component<NetConnection>>(out var component))
            {
                connection = component.Entity;
                return true;
            }
            connection = null;
            return false;
        }

        public static bool GetConnection(
            this IComponentEntity entity, [NotNullWhen(true)] out NetConnection? connection)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Components.Get<Component<NetConnection>>(out var component))
            {
                connection = component.Entity;
                return true;
            }
            connection = null;
            return false;
        }
    }
}
