using System;
using System.Reflection;

namespace MinecraftServerSharp
{
    public static class MethodInfoExtensions
    {
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo)
            where TDelegate : Delegate
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var type = typeof(TDelegate);
            return (TDelegate)methodInfo.CreateDelegate(type);
        }
    }
}
