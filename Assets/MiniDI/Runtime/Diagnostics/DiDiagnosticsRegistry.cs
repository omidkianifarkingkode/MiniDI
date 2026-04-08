#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace MiniDI.Diagnostics
{
    // This tracks containers globally for the Editor Dashboard
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

        public static void Register(ServiceContainer container, string name, ServiceContainer parent)
        {
            var existingTracked = FindTracked(container, RootContainers);
            if (existingTracked != null)
            {
                existingTracked.Name = name;
                OnRegistryChanged?.Invoke();
                return;
            }

            var tracked = new TrackedContainer { Container = container, Name = name };

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
                    RootContainers.Add(tracked); // Fallback
                }
            }
            else
            {
                RootContainers.Add(tracked);
            }

            OnRegistryChanged?.Invoke();
        }

        public static void Unregister(ServiceContainer container)
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
#endif