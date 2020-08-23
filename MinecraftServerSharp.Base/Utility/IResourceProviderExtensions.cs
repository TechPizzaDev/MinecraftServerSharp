using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MinecraftServerSharp.Utility
{
    public static class IResourceProviderExtensions
    {
        public static StreamReader OpenResourceReader(
            this IResourceProvider provider, string resourceName, Encoding? encoding = null)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var stream = provider.OpenResource(resourceName);
            if (stream == null)
                throw new KeyNotFoundException();

            return new StreamReader(stream, encoding, leaveOpen: false);
        }
    }
}
