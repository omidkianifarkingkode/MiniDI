using UnityEngine;

namespace MiniDI.Samples
{
    public class MiniDISampleGame : MonoBehaviour
    {
        DiContainer _gameplayDiContianer;

        private IEnemySpawner _enemySpawner;

        private void Start()
        {
            // Resolve one closed generic immediately so it shows up on the Dashboard on play
            Debug.Log("⚙️ Resolving IRepository<PlayerProfile> during initialization...");
            var playerRepo = GameDiContainer.Instance.Resolver.Resolve<IRepository<PlayerProfile>>();
            playerRepo.Save(new PlayerProfile { Name = "Hero" });
        }

        // --- State Machine Logic ---

        private void StartGameplay()
        {
            _gameplayDiContianer = new GameObject("Gamplay DI-Container").AddComponent<GameplayDiContainer>();

            _enemySpawner = _gameplayDiContianer.Resolver.Resolve<IEnemySpawner>();

            Debug.Log("🟢 Starting Gameplay...");
        }

        private void SpawnEnemy()
        {
            _enemySpawner.SpawnEnemy();
        }

        private void EndGameplay()
        {
            Debug.Log("🔴 Ending Gameplay... Destroying Scope and Enemies.");

            _enemySpawner = null;

            // Clean up the Unity GameObjects we spawned
            foreach (var enemy in FindObjectsOfType<EnemyBehavior>())
            {
                Destroy(enemy.gameObject);
            }

            // Clean up the DI container
            Destroy(_gameplayDiContianer.gameObject);
        }

        // --- Simple UI for Testing ---

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 300));

            if (GUILayout.Button("Resolve GameSettings Repo", GUILayout.Height(40)))
            {
                Debug.Log("⚙️ Resolving IRepository<GameSettings> dynamically...");
                var settingsRepo = GameDiContainer.Instance.Resolver.Resolve<IRepository<GameSettings>>();
                settingsRepo.Save(new GameSettings { Volume = 0.75f });
            }

            if (_gameplayDiContianer == null)
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
