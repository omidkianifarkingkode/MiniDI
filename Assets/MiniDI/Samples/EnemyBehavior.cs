using UnityEngine;

namespace MiniDI.Samples
{
    // Attached to spawned enemies
    public class EnemyBehavior : MonoBehaviour
    {
        private IAudioService _audioService;
        private string _enemyName;

        // "Method Injection" for Unity objects that are Instantiate()'d
        public void Construct(IAudioService audioService, string enemyName)
        {
            _audioService = audioService;
            _enemyName = enemyName;

            // Use the injected service immediately
            _audioService.PlaySpawnSound(_enemyName);
        }
    }
}
