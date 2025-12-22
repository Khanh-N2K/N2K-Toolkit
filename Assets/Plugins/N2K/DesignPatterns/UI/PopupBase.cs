using System;
using UnityEngine;

namespace N2K
{
    public class PopupBase : PoolMember
    {
        [field: Header("=== POPUP BASE ===")]

        [field: Header("References")]

        [field: SerializeField]
        public PopupType Type { get; private set; }

        [field: Header("Data")]

        private bool _isInitialized = false;

        [field: Header("Events")]
        
        protected Action _onShowed;
        
        protected Action _onHidden;

        protected virtual void Initialize()
        {

        }

        public void SetCallbacks(Action onShowed, Action onHidden)
        {
            _onShowed = onShowed;
            _onHidden = onHidden;
        }

        #region ================================ ACTUAL SHOW/ HIDE ===================================
        public virtual void Show()
        {
            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }
            gameObject.SetActive(true);
            _onShowed?.Invoke();
        }
        public virtual void Hide()
        {
            gameObject.SetActive(false);
            _onHidden?.Invoke();
        }
        #endregion ------------------------------------------------------------------------------------

        #region ======================== TEMPORARLY SHOW/ HIDE UNDER TOP POPUP ========================
        public virtual void TempShowUnderTopPopup()
        {
            gameObject.SetActive(true);
        }
        public virtual void TempHideUnderTopPopup()
        {
            gameObject.SetActive(false);
        }
        #endregion ----------------------------------------------------------------------------------------
    }
}