using System.Collections.Generic;
using System.IO;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Data
{
    public class FileResourceProvider : IResourceProvider
    {
        public string Directory { get; }
        public bool IncludeDirectoryName { get; }

        public FileResourceProvider(string directory, bool includeDirectoryName)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentEmptyException(nameof(directory));

            Directory = directory;
            IncludeDirectoryName = includeDirectoryName;
        }

        public IEnumerable<string> GetResourceNames()
        {
            yield break;
        }

        public Stream? OpenResource(string resourceName)
        {
            return null;
        }
    }
}
