using System;
using UnityEngine;

namespace N2K
{
    public abstract class Base_Screen : MonoBehaviour
    {
        [Header("=== BASE SCREEN ===")]

        #region ___ SETTINGS ___

        [Header("Settings")]
        [SerializeField, Tooltip("If true, this screen is disabled and reused after hide. If false, it is destroyed and its Addressable handle can be released.")]
        private bool _poolAfterHide = true;

        public bool PoolAfterHide => _poolAfterHide;

        #endregion

        #region ___ DATA ___

        private bool _isInitialized = false;

        private Action _onShowed;

        private Action _onHidden;

        #endregion

        // --------------------------------------------------------
        protected virtual void Initialize()
        {
            
        }

        internal void SetCallbacks(Action onShowed, Action onHidden)
        {
            _onShowed = onShowed;
            _onHidden = onHidden;
        }

        internal virtual void OnShow()
        {
            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }

            gameObject.SetActive(true);
            _onShowed?.Invoke();
        }

        internal virtual void OnHide(Action onFinished)
        {
            gameObject.SetActive(false);

            onFinished?.Invoke();

            _onHidden?.Invoke();
            _onShowed = null;
            _onHidden = null;
        }
    }
}