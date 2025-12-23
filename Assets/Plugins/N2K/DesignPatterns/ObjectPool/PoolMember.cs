using UnityEngine;
using UnityEngine.Pool;

namespace N2K
{
    public class PoolMember : MonoBehaviour
    {
        [Header("=== POOL MEMBER ===")]


        #region ___ SETTINGS ___
        [Header("Settings")]

        [SerializeField]
        private int defaultCapacity;

        [SerializeField]
        private int maxSize;

        public int DefaultCapacity => defaultCapacity;

        public int MaxSize => maxSize;
        #endregion ___


        #region ___ DATA ___
        [Header("Data")]

        private ObjectPool<PoolMember> pool;

        public ObjectPool<PoolMember> Pool => pool;
        #endregion ___ DATA ___


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