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

        public static Type GetUnderlyingType(this MemberInfo member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            return member.MemberType switch
            {
                MemberTypes.Event => ((EventInfo)member).EventHandlerType ?? typeof(void),
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Method => ((MethodInfo)member).ReturnType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                _ => throw new ArgumentException(member.GetType() + " is not supported.")
            };
        }
    }
}
