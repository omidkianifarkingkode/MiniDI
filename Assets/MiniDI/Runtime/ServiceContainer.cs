using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MiniDI
{
    public class ServiceContainer : IServiceResolver, IServiceRegister, IDisposable
    {
        private ServiceDescriptorBase[] _services = new ServiceDescriptorBase[16];
        private readonly ServiceContainer _parent;
        private readonly ILogger _logger;

        // Root constructor
        private ServiceContainer(ILogger logger)
        {
            _logger = logger ?? Debug.unityLogger;
        }

        // Scope constructor
        private ServiceContainer(ServiceContainer parent)
        {
            _parent = parent;
            _logger = parent._logger;
        }

        public static ServiceContainerBuilder CreateBuilder(ILogger logger = null)
        {
            return new ServiceContainerBuilder(new ServiceContainer(logger));
        }

        public ServiceContainer CreateScope()
        {
            return new ServiceContainer(this);
        }

        private void Ensure(int id)
        {
            if (id >= _services.Length)
            {
                int newSize = Math.Max(_services.Length * 2, id + 1);
                Array.Resize(ref _services, newSize);
            }
        }

        public void Register<T>(T instance, bool overwrite = true) where T : class
        {
            var id = ServiceSlot<T>.Id;
            Ensure(id);

            var descriptor = (ServiceDescriptor<T>)_services[id] ?? new ServiceDescriptor<T>();
            descriptor.Register(instance, overwrite);
            _services[id] = descriptor;
        }

        public void Register<T>(Func<IServiceResolver, T> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true) where T : class
        {
            var id = ServiceSlot<T>.Id;
            Ensure(id);

            var descriptor = (ServiceDescriptor<T>)_services[id] ?? new ServiceDescriptor<T>();
            descriptor.Register(factory, lifetime, overwrite);
            _services[id] = descriptor;
        }

        // --- Auto Registration (Reflection) ---
        public void Register<TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true) where TImplementation : class
        {
            Register<TImplementation>(CreateAutoFactory<TImplementation>(), lifetime, overwrite);
        }

        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true)
            where TImplementation : class, TInterface
            where TInterface : class
        {
            var factory = CreateAutoFactory<TImplementation>();
            Register<TInterface>(resolver => factory(resolver), lifetime, overwrite);
        }

        // --- Unity Helpers ---
        public void RegisterComponent<T>(T component, bool overwrite = true) where T : MonoBehaviour
        {
            Register<T>(component, overwrite);
        }

        public void RegisterPrefab<T>(T prefab, bool overwrite = true) where T : MonoBehaviour
        {
            Register<T>(resolver => UnityEngine.Object.Instantiate(prefab), ServiceLifetime.Transient, overwrite);
        }

        public void RegisterForward<TFrom, TTo>(bool overwrite = true)
            where TTo : class, TFrom
            where TFrom : class
        {
            Register<TFrom>(resolver => resolver.Resolve<TTo>(), ServiceLifetime.Transient, overwrite);
        }

        // --- Resolution Logic ---
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

        internal ServiceDescriptor<T> GetDescriptor<T>() where T : class
        {
            var id = ServiceSlot<T>.Id;
            if (id < _services.Length && _services[id] != null)
                return (ServiceDescriptor<T>)_services[id];

            return _parent?.GetDescriptor<T>();
        }

        private Func<IServiceResolver, T> CreateAutoFactory<T>() where T : class
        {
            var type = typeof(T);
            var constructors = type.GetConstructors();

            if (constructors.Length == 0)
                throw new InvalidOperationException($"No public constructors found for {type}");

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

        public void Dispose()
        {
            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] is IDisposableDescriptor disposableDesc)
                {
                    var instance = disposableDesc.GetInstance();
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _services[i] = null;
            }
        }
    }
}
