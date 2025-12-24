using UnityEngine;

namespace MilesUtils
{
    public class Singleton<T> : MonoBehaviour
        where T : Component
    {
        protected static T instance;

        public static bool HasInstance => instance != null;

        public static T TryGetInstance() => HasInstance ? instance : null;

        private static readonly object _instanceLock = new object();

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (instance == null) // Double-check
                        {
                            // 确保在播放模式下才创建
                            if (!Application.isPlaying)
                            {
                                Debug.LogWarning($"[Singleton] Attempting to access Instance '{typeof(T).Name}' when application is not playing.");
                                return null;
                            }

                            instance = FindAnyObjectByType<T>();
                            if (instance == null)
                            {
                                var go = new GameObject($"{typeof(T).Name} [Auto-Generated]");
                                instance = go.AddComponent<T>();
                            }
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Make sure to call base.Awake() in override if you need awake.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton0()
        {
            if (!Application.isPlaying)
                return;

            instance = this as T;
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
                return;

            if (instance == null)
            {
                instance = this as T;
            }
            else if (instance != this)
            {
                Destroy(gameObject); // Destroy the duplicate instance
            }
        }
    }
}
