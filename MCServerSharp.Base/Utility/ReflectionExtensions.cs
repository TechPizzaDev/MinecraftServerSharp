using System;
using System.Linq;
using System.Reflection;

namespace MCServerSharp.Utility
{
    public static class ReflectionExtensions
    {
        public static ParameterInfo[] GetDelegateParameters(this Type delegateType)
        {
            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

            if (!typeof(Delegate).IsAssignableFrom(delegateType))
                throw new ArgumentException("The type is not a delegate.", nameof(delegateType));

            // Simple trick to get delegate arguments.
            var invokeMethod = delegateType.GetMethod("Invoke")!;

            var methodParams = invokeMethod.GetParameters();
            return methodParams;
        }

        public static Type[] AsTypes(this ParameterInfo[] parameters)
        {
            return parameters.Select(x => x.ParameterType).ToArray();
        }
    }
}
