using System;
using System.Collections.Generic;
using UnityEditor;

namespace MiniDI.Editor.Diagnostics
{
    [InitializeOnLoad]
    public static class ServiceContainerEditorExtensions
    {
        private static readonly List<ServiceContainer> _activeContainers = new();

        static ServiceContainerEditorExtensions()
        {
            // 1. Clear dead containers when exiting Play Mode
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                {
                    _activeContainers.Clear();
                }
            };

            // 2. Track when created
            ServiceContainerEvents.OnContainerCreated += (container, parent) => TrackContainer(container);

            // 3. FALLBACK: Track when renamed (triggered by .Build()). 
            // This catches containers if the OnCreated event was missed!
            ServiceContainerEvents.OnContainerRenamed += (container, name) => TrackContainer(container);

            // 4. Remove when disposed
            ServiceContainerEvents.OnContainerDisposed += container =>
            {
                _activeContainers.Remove(container);
            };
        }

        private static void TrackContainer(ServiceContainer container)
        {
            if (container != null && !_activeContainers.Contains(container))
            {
                _activeContainers.Add(container);
            }
        }

        public static ServiceContainer GetRootContainer()
        {
            // Failsafe: remove any null references that might have snuck in
            _activeContainers.RemoveAll(c => c == null);

            return _activeContainers.Count > 0 ? _activeContainers[0] : null;
        }

        public static List<ServiceContainer> GetActiveContainers()
        {
            _activeContainers.RemoveAll(c => c == null);
            return _activeContainers;
        }

        public static IEnumerable<ServiceDiagnosticInfo> GetDiagnostics(this ServiceContainer container)
        {
            var diagnostics = new List<ServiceDiagnosticInfo>();

            // 1. Read the internal _services array
            if (container.Services != null)
            {
                foreach (var descriptor in container.Services)
                {
                    if (descriptor is IDiagnosticDescriptor diag)
                    {
                        diagnostics.Add(new ServiceDiagnosticInfo
                        {
                            ServiceType = diag.ServiceType,
                            ImplementationType = diag.ImplementationType,
                            Lifetime = diag.Lifetime,
                            IsInstantiated = diag.IsInstantiated,
                            Dependencies = diag.Dependencies ?? Array.Empty<Type>()
                        });
                    }
                }
            }

            // 2. Read the internal _openGenerics dictionary
            if (container.OpenGenerics != null)
            {
                foreach (var kvp in container.OpenGenerics)
                {
                    diagnostics.Add(new ServiceDiagnosticInfo
                    {
                        ServiceType = kvp.Key,
                        ImplementationType = kvp.Value.ImplementationType,
                        Lifetime = kvp.Value.Lifetime,
                        IsInstantiated = false,
                        Dependencies = Array.Empty<Type>()
                    });
                }
            }

            return diagnostics;
        }
    }
}
