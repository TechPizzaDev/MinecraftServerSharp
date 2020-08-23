using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinecraftServerSharp.Collections;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Data
{
    public class FallbackResourceProvider : IResourceProvider
    {
        public ReadOnlyList<IResourceProvider> Providers { get; }

        public FallbackResourceProvider(IEnumerable<IResourceProvider> providers)
        {
            var list = new List<IResourceProvider>(providers);
            list.Remove(null!);

            Providers = new ReadOnlyList<IResourceProvider>(list);
        }

        public FallbackResourceProvider(params IResourceProvider[] providers) : 
            this((IEnumerable<IResourceProvider>)providers)
        {
        }

        public IEnumerable<string> GetResourceNames()
        {
            return Providers.SelectMany(x => x.GetResourceNames());
        }

        public Stream? OpenResource(string resourceName)
        {
            return Providers.Select(x => x.OpenResource(resourceName)).FirstOrDefault(x => x != null);
        }
    }
}
