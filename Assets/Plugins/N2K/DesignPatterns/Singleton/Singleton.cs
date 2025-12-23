using UnityEngine;

namespace N2K
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError($"There's more than 1 instance of {typeof(T)} existed!");
                Destroy(gameObject);
            }
            else
            {
                Instance = (T)this;
            }
        }
    }
}