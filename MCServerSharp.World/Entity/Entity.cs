using System;
using MCServerSharp.Components;
using MCServerSharp.World;

namespace MCServerSharp.Entities
{
    public class Entity : ComponentEntity
    {
        public Dimension Dimension { get; private set; }

        public Entity(Dimension dimension)
        {
            Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));
        }

        public void TravelToDimension(Dimension target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Dimension = target;
        }
    }
}
