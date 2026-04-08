using System;
using System.Linq;
using System.Reflection;

namespace MiniDI
{
    internal static class ServiceFactory
    {
        public static Func<IServiceResolver, T> Create<T>() where T : class
        {
            var type = typeof(T);
            var constructors = type.GetConstructors();

            if (constructors.Length == 0)
                throw new InvalidOperationException($"No public constructors found for {type}");

            // Find the constructor with the most parameters
            var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = ctor.GetParameters();

            var resolveMethod = typeof(IServiceResolver).GetMethod(nameof(IServiceResolver.Resolve));
            var paramResolvers = new Func<IServiceResolver, object>[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var genericResolve = resolveMethod.MakeGenericMethod(paramType);
                paramResolvers[i] = (resolver) => genericResolve.Invoke(resolver, null);
            }

            return (resolver) =>
            {
                var args = new object[paramResolvers.Length];
                for (int i = 0; i < paramResolvers.Length; i++)
                    args[i] = paramResolvers[i](resolver);

                return (T)ctor.Invoke(args);
            };
        }
    }
}
