using System;
using UnityEngine;

namespace N2K
{
    public class PopupBase : MonoBehaviour
    {
        [Header("=== POPUP BASE ===")]


        #region ___ SETTINGS ___
        [Header("Settings")]

        [SerializeField]
        private PopupType _type;

        [SerializeField]
        private bool _destroyAfterHide = false;

        public PopupType Type => _type;

        public bool DestroyAfterHide => _destroyAfterHide;
        #endregion ___


        #region ___ DATA ___
        private bool _isInitialized = false;

        private Action _onShowed;
        
        private Action _onHidden;
        #endregion ___


        protected virtual void Initialize()
        {

        }

        public void SetCallbacks(Action onShowed, Action onHidden)
        {
            _onShowed = onShowed;
            _onHidden = onHidden;
        }

        public void Hide()
        {
            UIManager.Instance.HidePopup(this);
        }

        public virtual void OnShow()
        {
            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }
            gameObject.SetActive(true);
            _onShowed?.Invoke();
        }

        public virtual void OnHide()
        {
            gameObject.SetActive(false);
            _onHidden?.Invoke();
        }
    }
}