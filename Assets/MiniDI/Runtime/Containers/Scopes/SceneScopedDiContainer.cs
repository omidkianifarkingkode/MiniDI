using UnityEngine;

namespace MiniDI
{
    public abstract class SceneScopedDiContainer : DiContainer
    {
        [Header("Scene Scope Settings")]
        [Tooltip("If null, it will automatically parent to the Global container.")]
        [SerializeField] private DiContainer parentScope;

        protected override ServiceContainerBuilder CreateBuilder()
        {
            ServiceContainer parentContainer = null;

            // 1. Try manual parent
            if (parentScope != null)
            {
                parentContainer = parentScope.Container;
            }
            // 2. Fallback to Global
            else if (ServiceContainer.Global != null)
            {
                parentContainer = ServiceContainer.Global;
            }

            // Create scope if parent exists, otherwise become a root container
            return parentContainer != null
                ? ServiceContainer.CreateScope(parentContainer, containerName)
                : ServiceContainer.CreateGlobal(containerName);
        }
    }
}
