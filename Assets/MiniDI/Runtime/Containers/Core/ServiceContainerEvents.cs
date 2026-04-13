using System;

namespace MiniDI
{
    public static class ServiceContainerEvents
    {
        // Fired when a container is instantiated (Container, Parent Container)
        public static event Action<ServiceContainer, ServiceContainer> OnContainerCreated;

        // Fired when a container is named via the Builder (Container, Name)
        public static event Action<ServiceContainer, string> OnContainerRenamed;

        // Fired when a container is disposed
        public static event Action<ServiceContainer> OnContainerDisposed;

        internal static void NotifyCreated(ServiceContainer container, ServiceContainer parent)
            => OnContainerCreated?.Invoke(container, parent);

        internal static void NotifyRenamed(ServiceContainer container, string name)
            => OnContainerRenamed?.Invoke(container, name);

        internal static void NotifyDisposed(ServiceContainer container)
            => OnContainerDisposed?.Invoke(container);
    }
}
