using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniDI
{
    public sealed class ServiceContainerBuilder
    {
        private readonly ServiceContainer _container;
        private readonly List<Action<IServiceResolver>> _initializeActions = new();
        private bool _validateOnBuild = true;
        private string _containerName = "Unnamed Container";

        internal ServiceContainerBuilder(ServiceContainer container)
        {
            _container = container;
        }

        public ServiceContainerBuilder WithName(string name)
        {
            _containerName = name;
            
            return this;
        }

        public ServiceContainerBuilder ConfigureServices(Action<IServiceRegister> configure)
        {
            configure?.Invoke(_container);
            return this;
        }

        /// <summary>
        /// Validates the dependency graph for missing registrations or circular dependencies.
        /// Throws an exception if the graph is invalid.
        /// </summary>
        private ServiceContainerBuilder Validate()
        {
            var allDescriptors = _container.GetAllDescriptors();
            var visited = new HashSet<Type>();
            var currentStack = new List<Type>();

            foreach (var descriptor in allDescriptors)
            {
                CheckRecursive(descriptor.ServiceType, visited, currentStack);
            }

            foreach (var openGenericImpl in _container.GetOpenGenericImplementations())
            {
                ValidateOpenGenericTemplate(openGenericImpl);
            }

            return this;
        }

        private void CheckRecursive(Type type, HashSet<Type> visited, List<Type> currentStack)
        {
            // 1. Circular Dependency Check
            if (currentStack.Contains(type))
            {
                currentStack.Add(type);
                var path = string.Join(" -> ", currentStack.Select(t => t.Name));
                throw new Exception($"DI Validation Failed: Circular dependency detected!\nPath: {path}");
            }

            // 2. Already validated this branch
            if (visited.Contains(type)) return;

            // 3. Check if the service exists in the container
            var descriptor = _container.GetDescriptor(type);
            if (descriptor == null)
            {
                var dependent = currentStack.Count > 0 ? currentStack.Last().Name : "Root";
                throw new Exception($"DI Validation Failed: Service '{type.Name}' is not registered, but is required by '{dependent}'.");
            }

            // 4. Traverse dependencies
            currentStack.Add(type);
            foreach (var dependency in descriptor.Dependencies)
            {
                CheckRecursive(dependency, visited, currentStack);
            }

            // Cleanup for backtracking
            currentStack.RemoveAt(currentStack.Count - 1);
            visited.Add(type);
        }

        private void ValidateOpenGenericTemplate(Type openGenericImplType)
        {
            // Get the constructor (assuming single constructor for DI)
            var constructors = openGenericImplType.GetConstructors();
            if (constructors.Length == 0) return;

            var ctor = constructors[0];

            foreach (var param in ctor.GetParameters())
            {
                Type depType = param.ParameterType;

                // We can only validate static dependencies (like IAudioService).
                // If the dependency contains generic parameters (like IList<T>), 
                // we skip it because we can't validate it until T is known.
                if (!depType.ContainsGenericParameters)
                {
                    // Skip the built-in resolver
                    if (depType == typeof(IServiceResolver)) continue;

                    var depDescriptor = _container.GetDescriptor(depType);
                    if (depDescriptor == null)
                    {
                        throw new Exception(
                            $"Validation Failed: Open Generic template '{openGenericImplType.GetNiceTypeName()}' " +
                            $"requires '{depType.GetNiceTypeName()}' in its constructor, but it is not registered!"
                        );
                    }
                }
            }
        }

        public ServiceContainerBuilder OnInitialize(Action<IServiceResolver> action)
        {
            if (action != null)
            {
                _initializeActions.Add(action);
            }

            return this;
        }

        public ServiceContainerBuilder DisableValidation()
        {
            _validateOnBuild = false;
            return this;
        }

        public ServiceContainer Build()
        {
            // 1. VALIDATION PHASE
            // Happens after all registrations, but before any instantiations!
            if (_validateOnBuild)
            {
                Validate();
            }

            // 2. INITIALIZATION PHASE
            // Now it is 100% safe to resolve services
            foreach (var action in _initializeActions)
            {
                action(_container);
            }

            _container.Name = _containerName;

            ServiceContainerEvents.NotifyRenamed(_container, _containerName);

            return _container;
        }
    }
}
