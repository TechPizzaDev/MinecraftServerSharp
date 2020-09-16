using System.Collections.Generic;
using System.IO;

namespace MCServerSharp.Utility
{
    public interface IResourceProvider
    {
        IEnumerable<string> GetResourceNames();

        Stream? OpenResource(string resourceName);
    }
}
