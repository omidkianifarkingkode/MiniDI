using System;
using System.Linq;
using System.Reflection;

namespace MiniDI
{
    internal static class ServiceFactory
    {
        // Now returns a tuple containing the factory and the detected dependencies
        public static (Func<IServiceResolver, T> Factory, Type[] Dependencies) Create<T>(Type implementationType) where T : class
        {
            var constructors = implementationType.GetConstructors();

            if (constructors.Length == 0)
                throw new InvalidOperationException($"No public constructors found for {implementationType.Name}");

            // Find the constructor with the most parameters (Greediest)
            var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = ctor.GetParameters();

            // Capture the types for the Validation step
            var dependencyTypes = parameters.Select(p => p.ParameterType).ToArray();

            var resolveMethod = typeof(IServiceResolver).GetMethod(nameof(IServiceResolver.Resolve));
            var paramResolvers = new Func<IServiceResolver, object>[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var genericResolve = resolveMethod.MakeGenericMethod(paramType);
                paramResolvers[i] = (resolver) => genericResolve.Invoke(resolver, null);
            }

            Func<IServiceResolver, T> factory = (resolver) =>
            {
                var args = new object[paramResolvers.Length];
                for (int i = 0; i < paramResolvers.Length; i++)
                    args[i] = paramResolvers[i](resolver);

                return (T)ctor.Invoke(args);
            };

            return (factory, dependencyTypes);
        }
    }
}
