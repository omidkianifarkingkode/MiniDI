using UnityEngine;

namespace MiniDI.Samples
{
    public interface IAudioService { void PlaySpawnSound(string enemyName); }

    // Registered as TRANSIENT. Every time this is resolved, a NEW instance is created.
    public class AudioService : IAudioService
    {
        private readonly int _audioSourceId;

        public AudioService()
        {
            _audioSourceId = Random.Range(1000, 9999); // Generate a random ID to prove it's Transient
        }

        public void PlaySpawnSound(string enemyName)
        {
            Debug.Log($"🔊 [AudioSource #{_audioSourceId}] Playing roar for {enemyName}!");
        }
    }
}
