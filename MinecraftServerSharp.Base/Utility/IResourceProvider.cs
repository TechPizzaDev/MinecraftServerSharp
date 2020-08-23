using System.Collections.Generic;
using System.IO;

namespace MinecraftServerSharp.Utility
{
    public interface IResourceProvider
    {
        IEnumerable<string> GetResourceNames();

        Stream? OpenResource(string resourceName);
    }
}
