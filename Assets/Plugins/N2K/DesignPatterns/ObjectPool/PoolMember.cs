using UnityEngine;
using UnityEngine.Pool;

namespace N2K
{
    public class PoolMember : MonoBehaviour
    {
        [Header("=== POOL MEMBER ===")]

        [Header("Settings")]
        [SerializeField]
        private int defaultCapacity;
        [SerializeField]
        private int maxSize;
        public int DefaultCapacity => defaultCapacity;
        public int MaxSize => maxSize;

        [Header("Data")]
        private ObjectPool<PoolMember> pool;
        public ObjectPool<PoolMember> Pool => pool;

        public virtual void SetPool(ObjectPool<PoolMember> pool)
        {
            this.pool = pool;
        }

        #region ========================= POOL ACTION CALLBACKS ===================================

        public virtual void OnGetFromPool()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnReleaseToPool()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnDestroyFromPool()
        {
            gameObject.SetActive(false);
        }
        #endregion -----------------------------------------------------------------------------------

        public void ReleaseToPool()
        {
            pool.Release(this);
        }
    }
}