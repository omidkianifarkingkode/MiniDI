using UnityEngine;

namespace MiniDI.Samples
{
    public interface IRepository<T>
    {
        void Save(T item);
    }

    public class LocalRepository<T> : IRepository<T>
    {
        public LocalRepository(IAudioService service)
        {
            Debug.Log($"[LocalRepository] Created new repository instance for type: <color=cyan>{typeof(T).Name}</color>");
        }

        public void Save(T item)
        {
            Debug.Log($"[LocalRepository<{typeof(T).Name}>] Successfully saved item.");
        }
    }

    public class PlayerProfile
    {
        public string Name;
    }

    public class GameSettings
    {
        public float Volume;
    }
}
