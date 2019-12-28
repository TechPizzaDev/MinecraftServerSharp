using System;
using System.Reflection;

namespace MinecraftServerSharp
{
    public static class MethodInfoExtensions
    {
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo)
            where TDelegate : Delegate
        {
            return (TDelegate)methodInfo.CreateDelegate(typeof(TDelegate));
        }
    }
}
