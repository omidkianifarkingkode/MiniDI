using System;
using UnityEngine;

namespace MiniDI
{
    public interface IServiceRegister
    {
        // --- Existing Generic Methods (For normal types & CLOSED generics) ---
        void Register<T>(T instance, bool overwrite = true) where T : class;
        void Register<T>(Func<IServiceResolver, T> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true) where T : class;
        void Register<TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true) where TImplementation : class;
        void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true)
            where TImplementation : class, TInterface
            where TInterface : class;

        void RegisterComponent<T>(T component, bool overwrite = true) where T : MonoBehaviour;
        void RegisterPrefab<T>(T prefab, bool overwrite = true) where T : MonoBehaviour;
        void RegisterForward<TFrom, TTo>(bool overwrite = true) where TTo : class, TFrom where TFrom : class;

        // --- NEW METHOD (For OPEN generics and dynamic types) ---
        void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true);
    }
}
