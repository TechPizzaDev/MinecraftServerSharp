
namespace MCServerSharp.Components
{
    public class ComponentEntity : IComponentEntity
    {
        private ComponentCollection? _components;

        public ComponentCollection Components
        {
            get
            {
                if (_components == null)
                    _components = new ComponentCollection();
                return _components;
            }
        }

        public ComponentEntity()
        {
        }
    }
}
