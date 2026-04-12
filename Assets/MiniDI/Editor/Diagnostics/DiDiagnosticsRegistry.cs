using System;
using System.Collections.Generic;
using UnityEditor;
using MiniDI;

namespace MiniDI.Editor.Diagnostics
{
    [InitializeOnLoad]
    public static class DiDiagnosticsRegistry
    {
        public class TrackedContainer
        {
            public ServiceContainer Container;
            public string Name;
            public TrackedContainer Parent;
            public List<TrackedContainer> Children = new List<TrackedContainer>();
        }

        public static readonly List<TrackedContainer> RootContainers = new List<TrackedContainer>();
        public static event Action OnRegistryChanged;

        static DiDiagnosticsRegistry()
        {
            ServiceContainerEvents.OnContainerCreated += HandleContainerCreated;
            ServiceContainerEvents.OnContainerRenamed += HandleContainerRenamed;
            ServiceContainerEvents.OnContainerDisposed += HandleContainerDisposed;

            EditorApplication.playModeStateChanged += (state) => {
                // Clear the dashboard when Play Mode stops AND when it's about to start.
                // This prevents "ghost" containers if Domain Reload is disabled.
                if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                {
                    RootContainers.Clear();
                    OnRegistryChanged?.Invoke();
                }
            };
        }

        private static void HandleContainerCreated(ServiceContainer container, ServiceContainer parent)
        {
            // Prevent duplicates if the safety net already caught it
            if (FindTracked(container, RootContainers) != null) return;

            var tracked = new TrackedContainer { Container = container, Name = "Unnamed Container" };

            if (parent != null)
            {
                var parentTracked = FindTracked(parent, RootContainers);
                if (parentTracked != null)
                {
                    tracked.Parent = parentTracked;
                    parentTracked.Children.Add(tracked);
                }
                else
                {
                    // Safety Net: Parent exists but isn't tracked yet! Track it recursively.
                    HandleContainerCreated(parent, parent.Parent);

                    parentTracked = FindTracked(parent, RootContainers);
                    if (parentTracked != null)
                    {
                        tracked.Parent = parentTracked;
                        parentTracked.Children.Add(tracked);
                    }
                }
            }
            else
            {
                RootContainers.Add(tracked);
            }

            OnRegistryChanged?.Invoke();
        }

        private static void HandleContainerRenamed(ServiceContainer container, string name)
        {
            var tracked = FindTracked(container, RootContainers);

            if (tracked == null)
            {
                // SAFETY NET: We missed the Creation event due to Unity load orders!
                // Because Build() calls NotifyRenamed, we catch it here and reconstruct it.
                HandleContainerCreated(container, container.Parent);
                tracked = FindTracked(container, RootContainers);
            }

            if (tracked != null)
            {
                tracked.Name = name;
            }

            OnRegistryChanged?.Invoke();
        }

        private static void HandleContainerDisposed(ServiceContainer container)
        {
            var tracked = FindTracked(container, RootContainers);
            if (tracked != null)
            {
                if (tracked.Parent != null) tracked.Parent.Children.Remove(tracked);
                RootContainers.Remove(tracked);
            }
            OnRegistryChanged?.Invoke();
        }

        private static TrackedContainer FindTracked(ServiceContainer target, List<TrackedContainer> list)
        {
            foreach (var item in list)
            {
                if (item.Container == target) return item;
                var foundInChildren = FindTracked(target, item.Children);
                if (foundInChildren != null) return foundInChildren;
            }
            return null;
        }
    }
}
