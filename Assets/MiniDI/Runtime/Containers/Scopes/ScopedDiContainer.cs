using System;
using UnityEngine;

namespace MiniDI
{
    public abstract class ScopedDiContainer<TParent> : DiContainer where TParent : DiContainer
    {
        protected override ServiceContainerBuilder CreateBuilder()
        {
            // 1. Try to find the parent in the physical Unity hierarchy first
            TParent foundParent = GetComponentInParent<TParent>();

            // 2. If not found in hierarchy, search the entire scene
            if (foundParent == null)
            {
#if UNITY_2023_1_OR_NEWER
                foundParent = FindFirstObjectByType<TParent>();
#else
                foundParent = FindObjectOfType<TParent>();
#endif
            }

            ServiceContainer parentContainer = null;

            if (foundParent != null)
            {
                parentContainer = foundParent.Container;
            }
            else if (ServiceContainer.Global != null)
            {
                Debug.LogWarning($"[{containerName}] Could not find parent of type {typeof(TParent).Name}. Falling back to Global Scope.");
                parentContainer = ServiceContainer.Global;
            }

            if (parentContainer == null)
            {
                throw new InvalidOperationException($"[{containerName}] Failed to build: Parent scope {typeof(TParent).Name} not found, and no Global scope exists.");
            }

            return ServiceContainer.CreateScope(parentContainer, containerName);
        }
    }
}
