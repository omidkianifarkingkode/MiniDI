using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniDI
{
    public partial class ServiceContainer : IServiceRegister
    {
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
            descriptor.Register(factory, typeof(T), Array.Empty<Type>(), lifetime, overwrite);
            _services[id] = descriptor;
        }

        public void Register<TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true) where TImplementation : class
        {
            var id = ServiceSlot<TImplementation>.Id;
            Ensure(id);

            var descriptor = (ServiceDescriptor<TImplementation>)_services[id] ?? new ServiceDescriptor<TImplementation>();
            var (factory, deps) = ServiceFactory.Create<TImplementation>(typeof(TImplementation));

            descriptor.Register(factory, typeof(TImplementation), deps, lifetime, overwrite);
            _services[id] = descriptor;
        }

        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true)
            where TImplementation : class, TInterface
            where TInterface : class
        {
            var id = ServiceSlot<TInterface>.Id;
            Ensure(id);

            var (factory, deps) = ServiceFactory.Create<TImplementation>(typeof(TImplementation));
            var descriptor = (ServiceDescriptor<TInterface>)_services[id] ?? new ServiceDescriptor<TInterface>();

            descriptor.Register(resolver => factory(resolver), typeof(TImplementation), deps, lifetime, overwrite);
            _services[id] = descriptor;
        }

        public void RegisterComponent<T>(T component, bool overwrite = true) where T : MonoBehaviour
        {
            Register<T>(component, overwrite);
        }

        public void RegisterPrefab<T>(T prefab, bool overwrite = true) where T : MonoBehaviour
        {
            var id = ServiceSlot<T>.Id;
            Ensure(id);

            var descriptor = (ServiceDescriptor<T>)_services[id] ?? new ServiceDescriptor<T>();
            descriptor.Register(resolver => UnityEngine.Object.Instantiate(prefab), typeof(T), Array.Empty<Type>(), ServiceLifetime.Transient, overwrite);
            _services[id] = descriptor;
        }

        public void RegisterForward<TFrom, TTo>(bool overwrite = true)
            where TTo : class, TFrom
            where TFrom : class
        {
            var id = ServiceSlot<TFrom>.Id;
            Ensure(id);

            var descriptor = (ServiceDescriptor<TFrom>)_services[id] ?? new ServiceDescriptor<TFrom>();
            descriptor.Register(resolver => resolver.Resolve<TTo>(), typeof(TTo), new[] { typeof(TTo) }, ServiceLifetime.Transient, overwrite);
            _services[id] = descriptor;
        }

        public void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true)
        {
            if (serviceType.IsGenericTypeDefinition)
            {
                if (!implementationType.IsGenericTypeDefinition)
                {
                    throw new ArgumentException($"Service '{serviceType.Name}' is an open generic, but implementation '{implementationType.Name}' is not.");
                }

                _openGenerics ??= new Dictionary<Type, GenericRegistration>();
                _openGenerics[serviceType] = new GenericRegistration
                {
                    ImplementationType = implementationType,
                    Lifetime = lifetime
                };
                return;
            }

            var methodInfo = typeof(ServiceContainer).GetMethod(nameof(RegisterClosedGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = methodInfo.MakeGenericMethod(serviceType, implementationType);
            genericMethod.Invoke(this, new object[] { lifetime, overwrite });
        }

        private void RegisterClosedGeneric<TInterface, TImplementation>(ServiceLifetime lifetime, bool overwrite)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Register<TInterface, TImplementation>(lifetime, overwrite);
        }
    }
}
