using System;
using System.Reflection;

namespace MinecraftServerSharp
{
    public static class MethodInfoExtensions
    {
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo)
            where TDelegate : Delegate
        {
            var type = typeof(TDelegate);
            return (TDelegate)methodInfo.CreateDelegate(type);
        }
    }
}
