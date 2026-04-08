using System;
using UnityEngine;

namespace MiniDI
{
	public partial class ServiceContainer: IServiceRegister
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
			descriptor.Register(factory, lifetime, overwrite);
			_services[id] = descriptor;
		}

		public void Register<TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true) where TImplementation : class
		{
			var id = ServiceSlot<TImplementation>.Id;
			Ensure(id);

			var descriptor = (ServiceDescriptor<TImplementation>)_services[id] ?? new ServiceDescriptor<TImplementation>();

			descriptor.Register(ServiceFactory.Create<TImplementation>(), typeof(TImplementation), lifetime, overwrite);
			_services[id] = descriptor;
		}

		public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton, bool overwrite = true)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			var id = ServiceSlot<TInterface>.Id;
			Ensure(id);

			var factory = ServiceFactory.Create<TImplementation>();
			var descriptor = (ServiceDescriptor<TInterface>)_services[id] ?? new ServiceDescriptor<TInterface>();

			descriptor.Register(resolver => factory(resolver), typeof(TImplementation), lifetime, overwrite);
			_services[id] = descriptor;
		}

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
	}
}
