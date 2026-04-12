using UnityEngine;

namespace MiniDI.Samples
{
    [DefaultExecutionOrder(-1000)]
    public class MiniDISampleGame : MonoBehaviour
    {
        private ServiceContainer _globalContainer;
        private ServiceContainer _gameplayContainer; // Scoped child container

        private void Awake()
        {
            // 1. Setup Global Container
            _globalContainer = ServiceContainer.CreateGlobal("Global Root")
                .ConfigureServices(services =>
                {
                    // Audio is TRANSIENT: A new instance is created every time Resolve() is called.
                    services.Register<IAudioService, AudioService>(ServiceLifetime.Transient);

                    // Open Generic Registration ---
                    // Register the unbound generic types. The container will JIT compile 
                    // closed versions (like IRepository<PlayerProfile>) when requested.
                    services.Register(typeof(IRepository<>), typeof(LocalRepository<>), ServiceLifetime.Singleton);
                })
                .OnInitialize(resolver =>
                {
                    // Resolve one closed generic immediately so it shows up on the Dashboard on play
                    Debug.Log("⚙️ Resolving IRepository<PlayerProfile> during initialization...");
                    var playerRepo = resolver.Resolve<IRepository<PlayerProfile>>();
                    playerRepo.Save(new PlayerProfile { Name = "Hero" });
                })
                .Build();
        }

        private void OnDestroy()
        {
            EndGameplay(); // Ensure scope is cleaned up
            _globalContainer?.Dispose();
        }

        // --- State Machine Logic ---

        private void StartGameplay()
        {
            if (_gameplayContainer != null) return;

            Debug.Log("🟢 Starting Gameplay...");

            // Create a Child Scope from the Global Root
            _gameplayContainer = ServiceContainer.CreateScope(_globalContainer, "Gameplay Scope")
            .ConfigureServices(services =>
            {
                // Spawner is SCOPED: Only one spawner exists for the duration of this Gameplay session.
                services.Register<IEnemySpawner, EnemySpawner>(ServiceLifetime.Scoped);
            })
            // Resolve immediately to instantiate it so it shows on the Dashboard
            .OnInitialize(resolver => resolver.Resolve<IEnemySpawner>())
            .Build();
        }

        private void SpawnEnemy()
        {
            if (_gameplayContainer == null)
            {
                Debug.LogWarning("Cannot spawn enemy: Gameplay has not started!");
                return;
            }

            // Resolve the Scoped Spawner and tell it to spawn
            var spawner = _gameplayContainer.Resolve<IEnemySpawner>();
            spawner.SpawnEnemy();
        }

        private void EndGameplay()
        {
            if (_gameplayContainer == null) return;

            Debug.Log("🔴 Ending Gameplay... Destroying Scope and Enemies.");

            // Clean up the DI container
            _gameplayContainer.Dispose();
            _gameplayContainer = null;

            // Clean up the Unity GameObjects we spawned
            foreach (var enemy in FindObjectsOfType<EnemyBehavior>())
            {
                Destroy(enemy.gameObject);
            }
        }

        // --- Simple UI for Testing ---

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 300));

            if (GUILayout.Button("Resolve GameSettings Repo", GUILayout.Height(40)))
            {
                Debug.Log("⚙️ Resolving IRepository<GameSettings> dynamically...");
                var settingsRepo = _globalContainer.Resolve<IRepository<GameSettings>>();
                settingsRepo.Save(new GameSettings { Volume = 0.75f });
            }

            if (_gameplayContainer == null)
            {
                if (GUILayout.Button("Start Gameplay", GUILayout.Height(40))) StartGameplay();
            }
            else
            {
                if (GUILayout.Button("Spawn Enemy", GUILayout.Height(40))) SpawnEnemy();
                GUILayout.Space(20);
                if (GUILayout.Button("End Gameplay", GUILayout.Height(40))) EndGameplay();
            }

            GUILayout.EndArea();
        }
    }
}
