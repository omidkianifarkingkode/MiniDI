using System;
using System.Threading;

namespace MiniDI
{
    internal abstract class ServiceDescriptorBase { }

    internal interface IDisposableDescriptor
    {
        object GetInstance();
    }

    public interface IDiagnosticDescriptor
    {
        Type ServiceType { get; }
        Type ImplementationType { get; }
        ServiceLifetime Lifetime { get; }
        bool IsInstantiated { get; }
    }

    internal sealed class ServiceDescriptor<T> : ServiceDescriptorBase, IDisposableDescriptor, IDiagnosticDescriptor where T : class
    {
        public Func<IServiceResolver, T> Factory { get; private set; }
        public ServiceLifetime Lifetime { get; private set; }

        // Diagnostic Properties ---
        public Type ImplementationType { get; private set; }
        public Type ServiceType => typeof(T);
        public bool IsInstantiated => _instance != null;
        // ----------------------------------

        private T _instance;
        private bool _isResolving;

        public void Register(T instance, bool overwrite)
        {
            if (!overwrite && _instance != null) return;
            
            _instance = instance;
            Factory = null;
            Lifetime = ServiceLifetime.Singleton;
            ImplementationType = instance.GetType(); // Track the actual type
        }

        public void Register(Func<IServiceResolver, T> factory, ServiceLifetime lifetime, bool overwrite)
        {
            if (!overwrite && (Factory != null || _instance != null)) return;

            Factory = factory;
            Lifetime = lifetime;
            _instance = null;
            ImplementationType = null;
        }

        public void Register(Func<IServiceResolver, T> factory, Type implementationType, ServiceLifetime lifetime, bool overwrite)
        {
            Register(factory, lifetime, overwrite);
            ImplementationType = implementationType;
        }

        public T Resolve(IServiceResolver resolver)
        {
            if (Lifetime == ServiceLifetime.Transient)
                return Factory(resolver);

            if (_instance == null)
            {
                if (Factory == null)
                    throw new InvalidOperationException($"No factory registered for {typeof(T)}");

                if (_isResolving)
                    throw new InvalidOperationException($"Circular dependency detected for {typeof(T)}!");

                _isResolving = true;
                _instance = Factory(resolver);

                if (Lifetime == ServiceLifetime.Singleton)
                    Factory = null; // Free closure memory

                _isResolving = false;
            }

            return _instance;
        }

        public object GetInstance() => _instance;
    }

    internal static class ServiceSlot<T>
    {
        // Executes exactly once per type T, O(1) performance
        public static readonly int Id = ServiceTypeId.Next();
    }

    internal static class ServiceTypeId
    {
        private static int _nextId = -1;

        // Thread-safe ID generation
        public static int Next() => Interlocked.Increment(ref _nextId);
    }
}
