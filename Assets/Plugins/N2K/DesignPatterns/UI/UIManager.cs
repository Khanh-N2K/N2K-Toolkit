using System;
using System.Collections.Generic;
using UnityEngine;

namespace N2K
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("=== UI MANAGER ===")]


        #region ___ REFERENCES ___
        [Header("References - Holders")]

        [SerializeField]
        private Transform _screenHolder;

        [SerializeField]
        private Transform _popupHolder;

        [Header("References - Prefabs")]

        [SerializeField]
        private List<ScreenBase> _screenPrefabs;

        [SerializeField]
        private List<PopupBase> _popupPrefabs;
        #endregion ___


        #region ___ DATA ___
        private Dictionary<ScreenType, ScreenBase> _screenPrefabDict = new();

        private Dictionary<PopupType, PopupBase> _popupPrefabDict = new();

        private ScreenBase _currentScreen;

        private List<ScreenBase> _inactiveScreens = new List<ScreenBase>();

        private List<PopupBase> _activePopups = new List<PopupBase>();

        private List<PopupBase> _inactivePopups = new List<PopupBase>();

        public T GetCurrentScreen<T>() where T : ScreenBase => _currentScreen as T;
        #endregion ___


        protected override void Awake()
        {
            base.Awake();
            _screenPrefabDict = new Dictionary<ScreenType, ScreenBase>(_screenPrefabs.Count);
            _popupPrefabDict = new Dictionary<PopupType, PopupBase>(_popupPrefabs.Count);
            foreach (ScreenBase screen in _screenPrefabs)
            {
                _screenPrefabDict.Add(screen.Type, screen);
            }
            foreach (PopupBase popup in _popupPrefabs)
            {
                _popupPrefabDict.Add(popup.Type, popup);
            }
        }


        #region ___ QUERY METHODS ___
        public int GetActivePopupCountOfType(PopupType type)
        {
            int count = 0;
            foreach (var popup in _activePopups)
            {
                if (popup.Type == type)
                    count++;
            }
            return count;
        }

        public List<PopupBase> GetAllPopupsOfType(PopupType type)
        {
            List<PopupBase> result = new List<PopupBase>();
            foreach (var popup in _activePopups)
            {
                if (popup.Type == type)
                    result.Add(popup);
            }
            return result;
        }
        #endregion ___


        #region ___ SCREEN ___
        public ScreenBase ShowScreen(ScreenType type, Action onShowed = null, Action onHidden = null)
        {
            if (!_screenPrefabDict.ContainsKey(type))
            {
                Debug.LogError($"Screen type {type} not found in dictionary!");
                return null;
            }
            HideCurrentScreen();
            return ShowNewScreen();

            ScreenBase ShowNewScreen()
            {
                ScreenBase newScreen = GetAvailableScreen(type);
                newScreen.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                newScreen.SetCallbacks(onShowed, onHidden);
                newScreen.OnShow();
                _currentScreen = newScreen;
                return _currentScreen;
            }

            ScreenBase GetAvailableScreen(ScreenType type)
            {
                ScreenBase[] inactiveScreens = _inactiveScreens.ToArray();
                foreach (var screen in inactiveScreens)
                {
                    if (screen.Type == type)
                    {
                        _inactiveScreens.Remove(screen);
                        screen.gameObject.SetActive(true);
                        return screen;
                    }
                }
                return Instantiate(_screenPrefabDict[type], _screenHolder);
            }
        }

        public void HideCurrentScreen()
        {
            if (_currentScreen != null)
            {
                _currentScreen?.OnHide();
                if (_currentScreen.DestroyAfterHide)
                {
                    Destroy(_currentScreen.gameObject);
                }
                else
                {
                    _inactiveScreens.Add(_currentScreen);
                }
                _currentScreen = null;
            }
        }
        #endregion ___


        #region ___ POPUP ___
        public PopupBase ShowPopup(PopupType type, Action onShowed = null, Action onHidden = null)
        {
            if (!_popupPrefabDict.ContainsKey(type))
            {
                Debug.LogError($"Popup type {type} not found in dictionary!");
                return null;
            }
            PopupBase newPopup = GetAvailablePopup(type);
            newPopup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            newPopup.SetCallbacks(onShowed, onHidden);
            newPopup.transform.SetAsLastSibling();
            newPopup.OnShow();
            _activePopups.Add(newPopup);
            return newPopup;

            PopupBase GetAvailablePopup(PopupType type)
            {
                PopupBase[] inactivePopups = _inactivePopups.ToArray();
                foreach (var popup in inactivePopups)
                {
                    if (popup.Type == type)
                    {
                        _inactivePopups.Remove(popup);
                        popup.gameObject.SetActive(true);
                        return popup;
                    }
                }
                return Instantiate(_popupPrefabDict[type], _popupHolder);
            }
        }

        public void HideTopPopup()
        {
            if (_activePopups.Count > 0)
            {
                int lastIndex = _activePopups.Count - 1;
                PopupBase topPopup = _activePopups[lastIndex];
                _activePopups.RemoveAt(lastIndex);

                HidePopupInternal(topPopup);
            }
        }

        public void HidePopup(PopupBase targetPopup)
        {
            if (targetPopup == null || !_activePopups.Contains(targetPopup))
                return;

            _activePopups.Remove(targetPopup);
            HidePopupInternal(targetPopup);
        }

        private void HidePopupInternal(PopupBase popup)
        {
            if (popup == null)
                return;

            popup.OnHide();

            if (popup.DestroyAfterHide)
            {
                Destroy(popup.gameObject);
            }
            else
            {
                _inactivePopups.Add(popup);
            }
        }

        public void HideAllPopups()
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                PopupBase popup = _activePopups[i];
                HidePopupInternal(popup);
            }
            _activePopups.Clear();
        }

        public void HideAllPopupsOfType(PopupType type)
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                PopupBase popup = _activePopups[i];
                if (popup.Type == type)
                {
                    _activePopups.RemoveAt(i);
                    HidePopupInternal(popup);
                }
            }
        }
        #endregion ___
    }
}