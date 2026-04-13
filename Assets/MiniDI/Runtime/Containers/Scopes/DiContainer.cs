using UnityEngine;
using UnityEngine.Events;

namespace MiniDI
{
    public abstract class DiContainer : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] protected string containerName = "New Scope";
        [SerializeField] protected bool validateOnBuild = true;

        [Header("Events")]
        [Tooltip("Use this to register services via the Inspector (e.g., MonoBehaviours in the scene).")]
        public UnityEvent<IServiceRegister> onRegister;

        [Tooltip("Use this to resolve services or initialize logic after the container is built.")]
        public UnityEvent<IServiceResolver> onStarted;

        private ServiceContainer _container;
        public ServiceContainer Container => _container;

        // FEATURE 2: Provide service resolver property
        public IServiceResolver Resolver => _container;

        protected virtual void OnEnable()
        {
            // 1. Delegate builder creation to the specific subclass
            ServiceContainerBuilder builder = CreateBuilder();

            // 2. Registration Phase
            builder.ConfigureServices(InstallServices);
            builder.ConfigureServices(register => onRegister?.Invoke(register));

            // 3. Build Phase
            if (!validateOnBuild)
                builder.DisableValidation();

            _container = builder.Build();

            // 4. Resolution Event
            onStarted?.Invoke(_container);
        }

        /// <summary>
        /// Subclasses define how the builder is created (e.g., Global vs Scoped)
        /// </summary>
        protected abstract ServiceContainerBuilder CreateBuilder();

        /// <summary>
        /// Implement this to register C# classes.
        /// </summary>
        protected abstract void InstallServices(IServiceRegister services);

        protected virtual void OnDestroy()
        {
            _container?.Dispose();
        }
    }
}
