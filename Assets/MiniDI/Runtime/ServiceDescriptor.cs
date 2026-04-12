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
        Type[] Dependencies { get; }
    }

    internal sealed class ServiceDescriptor<T> : ServiceDescriptorBase, IDisposableDescriptor, IDiagnosticDescriptor where T : class
    {
        public Func<IServiceResolver, T> Factory { get; private set; }
        public ServiceLifetime Lifetime { get; private set; }
        public Type[] Dependencies { get; private set; } = Array.Empty<Type>();

        public Type ImplementationType { get; private set; }
        public Type ServiceType => typeof(T);
        public bool IsInstantiated => _instance != null;

        private T _instance;
        private bool _isResolving;

        public void Register(T instance, bool overwrite)
        {
            if (!overwrite && _instance != null) return;

            _instance = instance;
            Factory = null;
            Dependencies = Array.Empty<Type>(); // Instance already exists, no deps needed
            Lifetime = ServiceLifetime.Singleton;
            ImplementationType = instance.GetType();
        }

        public void Register(Func<IServiceResolver, T> factory, Type implementationType, Type[] dependencies, ServiceLifetime lifetime, bool overwrite)
        {
            if (!overwrite && (Factory != null || _instance != null)) return;

            Factory = factory;
            Dependencies = dependencies; // Capture dependencies for validation
            Lifetime = lifetime;
            _instance = null;
            ImplementationType = implementationType;
        }

        public T Resolve(IServiceResolver resolver)
        {
            if (_instance != null) return _instance;

            if (Factory == null)
                throw new InvalidOperationException($"No factory registered for {typeof(T).Name}");

            if (_isResolving)
                throw new CircularDependencyException(typeof(T));

            _isResolving = true;
            try
            {
                var instance = Factory(resolver);

                if (Lifetime != ServiceLifetime.Transient)
                {
                    _instance = instance;
                    if (Lifetime == ServiceLifetime.Singleton)
                        Factory = null;
                }

                return instance;
            }
            catch (CircularDependencyException ex)
            {
                ex.AddToPath(typeof(T));
                throw;
            }
            finally
            {
                _isResolving = false;
            }
        }

        public object GetInstance() => _instance;
    }


    internal static class ServiceSlot<T>
    {
        public static readonly int Id = ServiceTypeId.Next();
    }

    internal static class ServiceTypeId
    {
        private static int _nextId = -1;
        public static int Next() => Interlocked.Increment(ref _nextId);
    }
}