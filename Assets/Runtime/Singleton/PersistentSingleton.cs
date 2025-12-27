using UnityEngine;

namespace MilesUtils
{
    public class PersistentSingleton<T> : MonoBehaviour
        where T : Component
    {
        public bool AutoUnparentOnAwake = true;

        protected static T instance;
        private static readonly object _lock = new();
        private static volatile bool applicationIsQuitting = false;  // 使用 volatile
        public static bool HasInstance => instance != null;

        public static T TryGetInstance() => HasInstance ? instance : null;

        public static T Instance
        {
            get
            {
                // 应用退出时不再创建新实例
                if (applicationIsQuitting)
                {
                    Debug.LogWarning($"[PersistentSingleton] Instance '{typeof(T).Name}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                if (instance == null)
                {
                    lock (_lock)
                    {
                        // 双重检查，包括退出标志
                        if (instance == null && !applicationIsQuitting)
                        {
                            // 检查是否在播放模式（编辑器或运行时）
                            if (!Application.isPlaying)
                            {
                                Debug.LogWarning($"[PersistentSingleton] Attempting to access Instance '{typeof(T).Name}' when application is not playing.");
                                return null;
                            }

                            instance = FindAnyObjectByType<T>();
                            if (instance == null)
                            {
                                var go = new GameObject($"[{typeof(T).Name}] [Auto-Generated]");
                                instance = go.AddComponent<T>();
                            }
                        }
                    }
                }

                return instance;
            }
        }

        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
                return;

            if (AutoUnparentOnAwake)
            {
                transform.SetParent(null);
            }

            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                if (instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            // 只在非退出状态下清理引用
            if (!applicationIsQuitting && instance == this)
            {
                instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            // 标记应用正在退出，防止创建新实例
            applicationIsQuitting = true;

            // 清理静态引用
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}