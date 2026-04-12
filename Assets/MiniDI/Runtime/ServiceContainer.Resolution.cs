using System;

namespace MiniDI
{
    public partial class ServiceContainer : IServiceResolver
    {
        public T Resolve<T>() where T : class
        {
            var id = ServiceSlot<T>.Id;

            // 1. Local Scope
            if (id < _services.Length && _services[id] != null)
                return ((ServiceDescriptor<T>)_services[id]).Resolve(this);

            // 2. Parent Scope
            if (_parent != null)
            {
                var descriptor = _parent.GetDescriptor<T>();
                if (descriptor != null)
                {
                    if (descriptor.Lifetime == ServiceLifetime.Singleton)
                        return _parent.Resolve<T>();

                    if (descriptor.Lifetime == ServiceLifetime.Scoped)
                    {
                        Register(descriptor.Factory, ServiceLifetime.Scoped);
                        return Resolve<T>();
                    }

                    if (descriptor.Lifetime == ServiceLifetime.Transient)
                        return descriptor.Factory(this);
                }
            }

            // 3. Check if this is an Open Generic that needs to be compiled
            if (TryCreateClosedGeneric<T>())
            {
                return Resolve<T>();
            }

            throw new InvalidOperationException($"Service of type {typeof(T)} is not registered.");
        }

        public bool TryResolve<T>(out T value) where T : class
        {
            try
            {
                value = Resolve<T>();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        // JIT Compiler for Open Generics ---
        private bool TryCreateClosedGeneric<T>() where T : class
        {
            var type = typeof(T);
            if (!type.IsGenericType)
                return false;

            var openGenericType = type.GetGenericTypeDefinition();

            // Search for the open generic registration (start local, go to global)
            GenericRegistration reg = default;
            bool found = false;
            ServiceContainer targetContainer = this;

            while (targetContainer != null)
            {
                if (targetContainer._openGenerics != null && targetContainer._openGenerics.TryGetValue(openGenericType, out reg))
                {
                    found = true;
                    break;
                }
                targetContainer = targetContainer._parent;
            }

            if (!found)
                return false;

            // Found it! Compile the closed implementation type (e.g., ListRepository<int>)
            var closedImplementationType = reg.ImplementationType.MakeGenericType(type.GetGenericArguments());

            // Register it into the container where the open generic was defined.
            // This ensures Singletons are stored globally instead of in child scopes!
            var methodInfo = typeof(ServiceContainer).GetMethod(nameof(RegisterClosedGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = methodInfo.MakeGenericMethod(type, closedImplementationType);

            // Invoke the high-performance generic registration method using reflection just ONCE.
            genericMethod.Invoke(targetContainer, new object[] { reg.Lifetime, true });

            return true;
        }
    }
}
