using System;
using UnityEngine;

namespace N2K
{
    public class ScreenBase : MonoBehaviour
    {
        [Header("=== SCREEN BASE ===")]


        #region ___ SETTINGS ___
        [Header("Settings")]

        [SerializeField]
        private ScreenType _type;

        [SerializeField]
        private bool _destroyAfterHide = false;

        public ScreenType Type => _type;

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
            _onShowed = null;
            _onHidden = null;
        }
    }
}