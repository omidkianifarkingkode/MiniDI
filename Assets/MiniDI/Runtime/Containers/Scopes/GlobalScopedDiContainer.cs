using UnityEngine;

namespace MiniDI
{
    [DefaultExecutionOrder(-1000)]
    public abstract class GlobalScopedDiContainer : DiContainer // Ensure your base is named this
    {
        protected virtual void Awake()
        {
            // Set the name before building
            if (string.IsNullOrEmpty(containerName) || containerName == "New Scope")
            {
                containerName = "Global DI-Container";
            }

            // Ensure the Global scope persists across scene loads
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }

        protected override ServiceContainerBuilder CreateBuilder()
        {
            return ServiceContainer.CreateGlobal(containerName);
        }
    }

    public abstract class GlobalScopedDiContainer<T> : GlobalScopedDiContainer where T : GlobalScopedDiContainer<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        GameObject go = new($"(Runtime) {typeof(T).Name}");
                        _instance = go.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected override void Awake()
        {
            // Safety: If an instance already exists and it's not this one, destroy this one.
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[MiniDI] Duplicate {typeof(T).Name} found on {gameObject.name}. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            base.Awake();
        }
    }
}
