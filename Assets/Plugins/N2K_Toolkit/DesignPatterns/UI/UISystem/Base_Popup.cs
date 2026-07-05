using System;
using UnityEngine;

namespace N2K
{
    public abstract class Base_Popup : MonoBehaviour
    {
        [Header("=== BASE POPUP ===")]

        [SerializeField, Tooltip("If true, this popup is disabled and reused after hide. If false, it is destroyed and its Addressable handle can be released.")]
        private bool _poolAfterHide = true;
        public bool PoolAfterHide => _poolAfterHide;

        #region ___ DATA ___

        private bool _isInitialized = false;

        private Action _onShowed;

        private Action _onHidden;

        #endregion

        // ---------------------------------------------------------

        protected virtual void Initialize()
        {

        }

        internal void SetCallbacks(Action onShowed, Action onHidden)
        {
            _onShowed = onShowed;
            _onHidden = onHidden;
        }

        public void Hide()
        {
            UIManager.Instance.HidePopup(this);
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