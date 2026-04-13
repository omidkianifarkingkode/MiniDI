using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniDI
{
    public partial class ServiceContainer : IDisposable
    {
        private ServiceDescriptorBase[] _services = new ServiceDescriptorBase[16];
        private readonly ServiceContainer _parent;
        private readonly ILogger _logger;

        private Dictionary<Type, GenericRegistration> _openGenerics;

        // EXPOSE INTERNALS FOR THE EDITOR PACKAGE TO READ
        internal ServiceDescriptorBase[] Services => _services;
        internal Dictionary<Type, GenericRegistration> OpenGenerics => _openGenerics;
        internal ServiceContainer Parent => _parent;
        internal string Name { get; set; }

        internal struct GenericRegistration
        {
            public Type ImplementationType;
            public ServiceLifetime Lifetime;
        }

        // Static reference to the active global container
        public static ServiceContainer Global { get; private set; }

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

            var resolverId = ServiceSlot<IServiceResolver>.Id;
            Ensure(resolverId);

            var resolverDescriptor = new ServiceDescriptor<IServiceResolver>();
            resolverDescriptor.Register(this, true);
            _services[resolverId] = resolverDescriptor;

            ServiceContainerEvents.NotifyCreated(this, parent);
        }

        /// <summary>
        /// Creates the Global Root container builder.
        /// </summary>
        public static ServiceContainerBuilder CreateGlobal(string name = "Global Root", ILogger logger = null)
        {
            var container = new ServiceContainer(logger);
            Global = container; // Set as the ambient global container

            return new ServiceContainerBuilder(container).WithName(name);
        }

        /// <summary>
        /// Creates a Scoped container builder attached to the ambient Global container.
        /// </summary>
        public static ServiceContainerBuilder CreateScope(string name)
        {
            if (Global == null)
            {
                throw new InvalidOperationException("You must call ServiceContainer.CreateGlobal() before creating a scope this way.");
            }

            return CreateScope(Global, name);
        }

        /// <summary>
        /// Creates a Scoped container builder explicitly attached to a specific parent.
        /// </summary>
        public static ServiceContainerBuilder CreateScope(ServiceContainer parent, string name)
        {
            var childContainer = new ServiceContainer(parent);
            return new ServiceContainerBuilder(childContainer).WithName(name);
        }

        /// <summary>
        /// Creates a standalone Root container builder that is NOT set as the global container.
        /// Useful for internal module containers that need isolation.
        /// </summary>
        public static ServiceContainerBuilder Create(string name, ILogger logger = null)
        {
            var container = new ServiceContainer(logger);
            return new ServiceContainerBuilder(container).WithName(name);
        }

        private void Ensure(int id)
        {
            if (id >= _services.Length)
            {
                int newSize = Math.Max(_services.Length * 2, id + 1);
                Array.Resize(ref _services, newSize);
            }
        }

        internal ServiceDescriptor<T> GetDescriptor<T>() where T : class
        {
            var id = ServiceSlot<T>.Id;
            if (id < _services.Length && _services[id] != null)
                return (ServiceDescriptor<T>)_services[id];

            return _parent?.GetDescriptor<T>();
        }

        internal IDiagnosticDescriptor GetDescriptor(Type type)
        {
            // Check direct registrations first (O(n) search for validation is fine)
            foreach (var descriptor in GetAllDescriptors())
            {
                if (descriptor.ServiceType == type)
                    return descriptor;
            }

            // Check if it's a closed generic that can be resolved by our Open Generics
            if (type.IsGenericType && _openGenerics != null)
            {
                var def = type.GetGenericTypeDefinition();
                if (_openGenerics.ContainsKey(def))
                {
                    // During validation, we can simulate the registration of the closed generic
                    // so that the validator can "see" it.
                    return TryCreateClosedDescriptor(type);
                }
            }

            // Check parent containers
            return Parent?.GetDescriptor(type);
        }

        private IDiagnosticDescriptor TryCreateClosedDescriptor(Type closedType)
        {
            // This logic mirrors your Resolve logic for Open Generics
            // but returns the descriptor instead of the instance
            var method = typeof(ServiceContainer).GetMethod("TryCreateClosedGeneric",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(closedType.GetGenericArguments());

            // This will trigger the registration of the closed type internally
            genericMethod.Invoke(this, null);

            // Now return it
            return GetAllDescriptors().FirstOrDefault(d => d.ServiceType == closedType);
        }

        internal IEnumerable<IDiagnosticDescriptor> GetAllDescriptors()
        {
            if (_services == null) yield break;

            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] is IDiagnosticDescriptor diagnostic)
                {
                    yield return diagnostic;
                }
            }
        }

        internal IEnumerable<Type> GetOpenGenericImplementations()
        {
            if (_openGenerics == null) yield break;

            foreach (var genericDef in _openGenerics.Values)
            {
                yield return genericDef.ImplementationType;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] is IDisposableDescriptor disposableDesc)
                {
                    var instance = disposableDesc.GetInstance();
                    if (instance is IDisposable disposable && !ReferenceEquals(instance, this))
                    {
                        disposable.Dispose();
                    }
                }
                _services[i] = null;
            }

            ServiceContainerEvents.NotifyDisposed(this);
        }
    }
}
