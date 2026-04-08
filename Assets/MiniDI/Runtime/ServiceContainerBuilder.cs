using System;

namespace MiniDI
{
    public sealed class ServiceContainerBuilder
    {
        private readonly ServiceContainer _container;
        private Action<IServiceResolver> _onInitialize;
        private string _containerName = "Global Container";

        public ServiceContainerBuilder(ServiceContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// (Editor/Diagnostics) Assigns a custom name to this container for the Dashboard.
        /// </summary>
        public ServiceContainerBuilder WithName(string name)
        {
            _containerName = name;
            return this;
        }

        /// <summary>
        /// Registration Phase: Add services to the container.
        /// </summary>
        public ServiceContainerBuilder ConfigureServices(Action<IServiceRegister> configure)
        {
            configure?.Invoke(_container);
            return this;
        }

        /// <summary>
        /// Initialization Phase: Runs immediately after the container is built.
        /// Useful for resolving and caching initial singletons.
        /// </summary>
        public ServiceContainerBuilder OnInitialize(Action<IServiceResolver> initialize)
        {
            _onInitialize += initialize;
            return this;
        }

        /// <summary>
        /// Finalizes the container, runs initialization hooks, and returns the ready-to-use container.
        /// </summary>
        public ServiceContainer Build()
        {
#if UNITY_EDITOR
            // Register with diagnostics right before finalizing
            _container.RegisterWithDiagnostics(_containerName);
#endif
            _onInitialize?.Invoke(_container);
            return _container;
        }
    }
}
