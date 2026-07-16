using UnityEngine;
using UnityEngine.Pool;

namespace N2K
{
    public abstract class PoolMember : MonoBehaviour
    {
        private ObjectPool<PoolMember> pool;
        public ObjectPool<PoolMember> Pool => pool;

        protected abstract int defaultCapacity { get; }
        public int DefaultCapacity => defaultCapacity;

        protected abstract int maxSize { get; }
        public int MaxSize => maxSize;

        internal virtual void SetPool(ObjectPool<PoolMember> pool)
        {
            this.pool = pool;
        }

        #region ========================= POOL ACTION CALLBACKS ===================================

        internal virtual void OnGetFromPool()
        {
            gameObject.SetActive(true);
        }

        internal virtual void OnReleaseToPool()
        {
            gameObject.SetActive(false);
        }

        internal virtual void OnDestroyFromPool()
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