using System;
using UnityEngine;

namespace MiniDI
{
    public partial class ServiceContainer : IDisposable
    {
        private ServiceDescriptorBase[] _services = new ServiceDescriptorBase[16];
        private readonly ServiceContainer _parent;
        private readonly ILogger _logger;

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

#if UNITY_EDITOR
            RegisterWithDiagnostics("Scoped Container");
#endif

            var resolverId = ServiceSlot<IServiceResolver>.Id;
            Ensure(resolverId);

            var resolverDescriptor = new ServiceDescriptor<IServiceResolver>();
            resolverDescriptor.Register(this, true);
            _services[resolverId] = resolverDescriptor;
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

#if UNITY_EDITOR
            CleanupDiagnostics();
#endif
        }
    }
}
