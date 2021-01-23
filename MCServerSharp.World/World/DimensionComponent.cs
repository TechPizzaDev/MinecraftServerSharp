using MCServerSharp.Components;

namespace MCServerSharp.World
{
    public class DimensionComponent : Component<Dimension>
    {
        public Dimension Dimension => Entity;

        public DimensionComponent(Dimension dimension) : base(dimension)
        {
        }
    }
}
