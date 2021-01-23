
namespace MCServerSharp.Components
{
    public class ComponentEntity : IComponentEntity
    {
        public ComponentCollection Components { get; }

        public ComponentEntity()
        {
            Components = new ComponentCollection();
        }
    }
}
