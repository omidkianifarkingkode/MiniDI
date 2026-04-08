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
    }
}
