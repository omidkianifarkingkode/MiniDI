using UnityEngine;

namespace MiniDI.Samples
{
    public interface IEnemySpawner { void SpawnEnemy(); }

    // Registered as SCOPED in the Gameplay Container. 
    public class EnemySpawner : IEnemySpawner
    {
        private readonly IServiceResolver _resolver;
        private int _enemyCount = 0;

        // The spawner depends on the container resolver to pull fresh transient services
        public EnemySpawner(IServiceResolver resolver)
        {
            _resolver = resolver;
            Debug.Log("⚔️ Enemy Spawner Created!");
        }

        public void SpawnEnemy()
        {
            _enemyCount++;
            string enemyName = $"Goblin_{_enemyCount}";

            // Create a basic Unity GameObject
            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyObject.name = enemyName;
            enemyObject.transform.position = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));

            // Add our Enemy Behavior script
            var enemyBehavior = enemyObject.AddComponent<EnemyBehavior>();

            // RESOLVE a fresh Transient AudioService specifically for this enemy
            var enemyAudio = _resolver.Resolve<IAudioService>();

            // Inject dependencies into the Unity Component
            enemyBehavior.Construct(enemyAudio, enemyName);
        }
    }
}
