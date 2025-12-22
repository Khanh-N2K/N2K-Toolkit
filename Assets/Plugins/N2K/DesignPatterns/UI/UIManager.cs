using System;
using System.Collections.Generic;
using UnityEngine;

namespace N2K
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("Holders")]

        [SerializeField] 
        private Transform _screenHolder;

        [SerializeField] 
        private Transform _popupHolder;

        [Header("Prefabs")]

        [SerializeField] 
        private List<ScreenBase> _screenPrefabs;

        [SerializeField] 
        private List<PopupBase> _popupPrefabs;

        [Header("Data")]

        private Dictionary<ScreenType, ScreenBase> _screenDict = new();

        private Dictionary<PopupType, PopupBase> _popupDict = new();

        private ScreenBase _currentScreen;

        private Stack<PopupBase> _popupStack = new();

        public override void Initialize()
        {
            base.Initialize();

            foreach (ScreenBase screen in _screenPrefabs)
                _screenDict.Add(screen.Type, screen);
            foreach (PopupBase popup in _popupPrefabs)
                _popupDict.Add(popup.Type, popup);
        }

        #region ================================== SCREEN ====================================
        public void ShowScreen(ScreenType type, Action onShowed = null, Action onHidden = null)
        {
            HideCurrentScreen();

            ScreenBase newScreen = ObjectPoolAtlas.Instance.Get(_screenDict[type], _screenHolder) as ScreenBase;
            newScreen.SetCallbacks(onShowed, onHidden);
            newScreen.Show();
            _currentScreen = newScreen;
        }

        public void HideCurrentScreen()
        {
            if (_currentScreen != null)
            {
                _currentScreen?.Hide();
                _currentScreen.ReleaseToPool();
                _currentScreen = null;
            }
        }
        #endregion ------------------------------------------------------------------------------

        #region ======================================= POPUP ===================================
        public void ShowPopup(PopupType type, Action onActivated = null, Action onInactivated = null)
        {
            if (_popupStack.Count > 0)
                _popupStack.Peek().TempHideUnderTopPopup();

            PopupBase newPopup = ObjectPoolAtlas.Instance.Get(_popupDict[type], _popupHolder) as PopupBase;
            newPopup.SetCallbacks(onActivated, onInactivated);
            newPopup.Show();

            _popupStack.Push(newPopup);
        }

        public void HideTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                PopupBase topPopup = _popupStack.Pop();
                topPopup?.Hide();
                topPopup.ReleaseToPool();

                if (_popupStack.Count > 0)
                    _popupStack.Peek().TempShowUnderTopPopup();
            }
        }

        public void HidePopup(PopupBase targetPopup)
        {
            if (targetPopup == null || !_popupStack.Contains(targetPopup))
                return;

            Stack<PopupBase> buffer = new Stack<PopupBase>();

            while (_popupStack.Count > 0)
            {
                PopupBase top = _popupStack.Pop();
                if (top == targetPopup)
                {
                    top.Hide();
                    top.ReleaseToPool();
                    break;
                }
                else
                {
                    buffer.Push(top);
                }
            }

            while (buffer.Count > 0)
                _popupStack.Push(buffer.Pop());

            if (_popupStack.Count > 0)
                _popupStack.Peek().TempShowUnderTopPopup();
        }

        public void HideAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                PopupBase popup = _popupStack.Pop();
                popup?.Hide();
                popup.ReleaseToPool();
            }
        }
        #endregion -------------------------------------------------------------------------------
    }

    [Serializable]
    public enum ScreenType
    {
        Screen1 = 0,
        Screen2 = 1,
    }

    [Serializable]
    public enum PopupType
    {
        Popup1 = 0,
        Popup2 = 1,
    }
}