using System;
using UnityEngine;

namespace MiniDI
{
    public sealed class ServiceContainerBuilder
    {
        private readonly ServiceContainer _container;
        private Action<IServiceResolver> _onInitialize;

        public ServiceContainerBuilder(ServiceContainer container)
        {
            _container = container;
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
            _onInitialize?.Invoke(_container);
            return _container;
        }
    }
}
