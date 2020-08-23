using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Data
{
    public class AssemblyResourceProvider : IResourceProvider
    {
        private Dictionary<string, string> _resources;

        public Assembly Assembly { get; }
        public string RootNamespace { get; }

        public AssemblyResourceProvider(
            Assembly assembly,
            string? rootNamespace,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            RootNamespace = rootNamespace ?? "";
            RootNamespace = RootNamespace.TrimEnd('.');

            _resources = new Dictionary<string, string>(StringComparer.FromComparison(comparisonType));

            foreach (var name in Assembly.GetManifestResourceNames())
            {
                if (name.StartsWith(RootNamespace, comparisonType))
                {
                    int extraCutoff = 
                        name.Length > RootNamespace.Length && name[RootNamespace.Length] == '.' ? 1 : 0;
                    
                    string subName = name.Substring(RootNamespace.Length + extraCutoff);
                    string resourceName = ManifestNameToResourceName(subName, Path.DirectorySeparatorChar);
                    string altResourceName = ManifestNameToResourceName(subName, Path.AltDirectorySeparatorChar);

                    _resources.Add(resourceName, name);
                    _resources.Add(altResourceName, name);
                }
            }
        }

        public IEnumerable<string> GetResourceNames()
        {
            return _resources.Keys;
        }

        public Stream? OpenResource(string resourceName)
        {
            if (_resources.TryGetValue(resourceName, out var manifestName))
                return Assembly.GetManifestResourceStream(manifestName);
            return null;
        }

        public static string ResourceNameToManifestName(string resourceName)
        {
            if (resourceName == null)
                throw new ArgumentNullException(nameof(resourceName));

            return resourceName
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');
        }

        public static string ManifestNameToResourceName(string manifestName)
        {
            return ManifestNameToResourceName(manifestName, Path.DirectorySeparatorChar);
        }

        public static string ManifestNameToResourceName(string manifestName, char directorySeparator)
        {
            if (manifestName == null)
                throw new ArgumentNullException(nameof(manifestName));

            string? nameExtension = Path.GetExtension(manifestName);
            string nameWithoutExtension = manifestName.Substring(0, manifestName.Length - nameExtension?.Length ?? 0);

            string pathWithoutExtension = nameWithoutExtension.Replace('.', directorySeparator);
            return pathWithoutExtension + nameExtension;
        }
    }
}
