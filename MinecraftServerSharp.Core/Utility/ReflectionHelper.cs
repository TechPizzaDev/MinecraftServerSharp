using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MinecraftServerSharp.Utility
{
    public static class ReflectionHelper
    {
        public static TDelegate CreateDelegateFromMethod<TDelegate>(
            MethodInfo method, bool useFirstArgumentAsInstance)
               where TDelegate : Delegate
        {
            var delegateParams = typeof(TDelegate).GetDelegateParameters().AsTypes();
            var lambdaParams = delegateParams.Select(x => Expression.Parameter(x)).ToList();
            
            var call = useFirstArgumentAsInstance
                ? Expression.Call(lambdaParams[0], method, lambdaParams.Skip(1))
                : Expression.Call(method, lambdaParams);

            var lambda = Expression.Lambda<TDelegate>(call, lambdaParams);
            return lambda.Compile();
        }
    }
}
