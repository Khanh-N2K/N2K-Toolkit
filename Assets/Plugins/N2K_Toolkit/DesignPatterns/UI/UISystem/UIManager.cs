using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace N2K
{
    public class UIManager : Singleton<UIManager>
    {
        // --- REFERENCES ---
        [Header("=== UI MANAGER ===")]

        [Header("CAMERA")]
        [SerializeField]
        private Canvas _canvas;

        public Camera Camera => _canvas.worldCamera;

        [Header("HOLDERS")]
        [SerializeField]
        private Transform _screenHolder;

        [SerializeField]
        private Transform _popupHolder;

        [Header("PRELOAD ADDRESSABLES")]
        [SerializeField]
        private List<AssetReferenceGameObject> _preloadScreens = new();

        [SerializeField]
        private List<AssetReferenceGameObject> _preloadPopups = new();

#if UNITY_EDITOR
        [Header("EDITOR SETUP ONLY")]
        [Tooltip("Folder containing your ScreenBase prefabs.")]
        [SerializeField]
        private UnityEngine.Object _screenPrefabFolder;

        [Tooltip("Folder containing your PopupBase prefabs.")]
        [SerializeField]
        private UnityEngine.Object _popupPrefabFolder;
#endif


        // --- SETTINGS ---
        protected override bool IsDontDestroyOnLoad => false;


        // --- DATA ---
        private readonly Dictionary<Type, Base_Screen> _screenPrefabDict = new();

        private readonly Dictionary<Type, Base_Popup> _popupPrefabDict = new();

        private readonly Dictionary<Type, AsyncOperationHandle<GameObject>> _screenHandleDict = new();

        private readonly Dictionary<Type, AsyncOperationHandle<GameObject>> _popupHandleDict = new();

        private Base_Screen _currentScreen;

        private readonly List<Base_Screen> _inactiveScreens = new();

        private readonly List<Base_Popup> _activePopups = new();

        private readonly List<Base_Popup> _inactivePopups = new();

        private Task _preloadTask;

        public int ActivePopupCount { get; private set; } = 0;


        // --- ACTION ---
        public Action<Base_Screen> onScreenShown;

        public Action<Base_Screen> onScreenHidden;

        public Action<Base_Popup> onPopupShown;

        public Action<Base_Popup> onPopupHidden;


        protected override void OnSingletonAwake()
        {
            _preloadTask = PreloadAllAsync();
        }


        #region ___ PRELOAD ___

        private async Task EnsurePreloadCompleted()
        {
            if (_preloadTask != null)
            {
                await _preloadTask;
            }
        }

        private async Task PreloadAllAsync()
        {
            foreach (AssetReferenceGameObject assetRef in _preloadScreens)
            {
                await PreloadScreenAsync(assetRef);
            }

            foreach (AssetReferenceGameObject assetRef in _preloadPopups)
            {
                await PreloadPopupAsync(assetRef);
            }
        }

        private async Task PreloadScreenAsync(AssetReferenceGameObject assetRef)
        {
            if (assetRef == null || !assetRef.RuntimeKeyIsValid())
            {
                return;
            }

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetRef.RuntimeKey);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Failed to preload screen: {assetRef.RuntimeKey}");
                SafeRelease(handle);
                return;
            }

            Base_Screen prefab = handle.Result.GetComponent<Base_Screen>();

            if (prefab == null)
            {
                Debug.LogError($"Preloaded asset is not a ScreenBase: {handle.Result.name}");
                SafeRelease(handle);
                return;
            }

            Type type = prefab.GetType();

            if (_screenPrefabDict.ContainsKey(type))
            {
                Debug.LogError($"Duplicated screen preload type: {type.Name}");
                SafeRelease(handle);
                return;
            }

            _screenPrefabDict.Add(type, prefab);
            _screenHandleDict.Add(type, handle);
        }

        private async Task PreloadPopupAsync(AssetReferenceGameObject assetRef)
        {
            if (assetRef == null || !assetRef.RuntimeKeyIsValid())
            {
                return;
            }

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetRef.RuntimeKey);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Failed to preload popup: {assetRef.RuntimeKey}");
                SafeRelease(handle);
                return;
            }

            Base_Popup prefab = handle.Result.GetComponent<Base_Popup>();

            if (prefab == null)
            {
                Debug.LogError($"Preloaded asset is not a PopupBase: {handle.Result.name}");
                SafeRelease(handle);
                return;
            }

            Type type = prefab.GetType();

            if (_popupPrefabDict.ContainsKey(type))
            {
                Debug.LogError($"Duplicated popup preload type: {type.Name}");
                SafeRelease(handle);
                return;
            }

            _popupPrefabDict.Add(type, prefab);
            _popupHandleDict.Add(type, handle);
        }

        #endregion


        #region ___ LOAD PREFAB ___

        private async Task<Base_Screen> GetScreenPrefabAsync<T>() where T : Base_Screen
        {
            Type type = typeof(T);

            if (_screenPrefabDict.TryGetValue(type, out Base_Screen cachedPrefab))
            {
                return cachedPrefab;
            }

            string address = type.Name;

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Screen Addressable not found. Address: {address}");
                SafeRelease(handle);
                return null;
            }

            Base_Screen prefab = handle.Result.GetComponent<Base_Screen>();

            if (prefab == null)
            {
                Debug.LogError($"Addressable {address} does not have ScreenBase component.");
                SafeRelease(handle);
                return null;
            }

            if (prefab.GetType() != type)
            {
                Debug.LogError($"Addressable type mismatch. Address: {address}, Expected: {type.Name}, Got: {prefab.GetType().Name}");
                SafeRelease(handle);
                return null;
            }

            _screenPrefabDict.Add(type, prefab);
            _screenHandleDict.Add(type, handle);

            return prefab;
        }

        private async Task<Base_Popup> GetPopupPrefabAsync<T>() where T : Base_Popup
        {
            Type type = typeof(T);

            if (_popupPrefabDict.TryGetValue(type, out Base_Popup cachedPrefab))
            {
                return cachedPrefab;
            }

            string address = type.Name;

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Popup Addressable not found. Address: {address}");
                SafeRelease(handle);
                return null;
            }

            Base_Popup prefab = handle.Result.GetComponent<Base_Popup>();

            if (prefab == null)
            {
                Debug.LogError($"Addressable {address} does not have PopupBase component.");
                SafeRelease(handle);
                return null;
            }

            if (prefab.GetType() != type)
            {
                Debug.LogError($"Addressable type mismatch. Address: {address}, Expected: {type.Name}, Got: {prefab.GetType().Name}");
                SafeRelease(handle);
                return null;
            }

            _popupPrefabDict.Add(type, prefab);
            _popupHandleDict.Add(type, handle);

            return prefab;
        }

        #endregion


        #region ___ SCREEN ___

        public async Task<T> ShowScreen<T>(Action onShowed = null, Action onHidden = null) where T : Base_Screen
        {
            await EnsurePreloadCompleted();

            HideCurrentScreen();

            Base_Screen newScreen = GetInactiveScreen<T>();

            if (newScreen == null)
            {
                Base_Screen prefab = await GetScreenPrefabAsync<T>();

                if (prefab == null)
                {
                    return null;
                }

                newScreen = Instantiate(prefab, _screenHolder);
            }

            RectTransform rectTransform = newScreen.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            newScreen.SetCallbacks(onShowed, onHidden);
            newScreen.OnShow();

            _currentScreen = newScreen;

            onScreenShown?.Invoke(newScreen);

            return newScreen as T;
        }

        private Base_Screen GetInactiveScreen<T>() where T : Base_Screen
        {
            Type type = typeof(T);

            for (int i = _inactiveScreens.Count - 1; i >= 0; i--)
            {
                Base_Screen screen = _inactiveScreens[i];

                if (screen != null && screen.GetType() == type)
                {
                    _inactiveScreens.RemoveAt(i);
                    screen.gameObject.SetActive(true);
                    return screen;
                }
            }

            return null;
        }

        public void HideCurrentScreen()
        {
            if (_currentScreen == null)
            {
                return;
            }

            Base_Screen cachedScreen = _currentScreen;
            _currentScreen = null;

            cachedScreen.OnHide(() =>
            {
                onScreenHidden?.Invoke(cachedScreen);

                if (cachedScreen.PoolAfterHide)
                {
                    _inactiveScreens.Add(cachedScreen);
                }
                else
                {
                    Type type = cachedScreen.GetType();

                    Destroy(cachedScreen.gameObject);

                    TryReleaseScreenPrefab(type);
                }
            });
        }

        public bool IsScreenOpen<T>() where T : Base_Screen
        {
            return _currentScreen != null && _currentScreen.GetType() == typeof(T);
        }

        public bool TryGetCurrentScreen<T>(out T screen) where T : Base_Screen
        {
            if (_currentScreen != null && _currentScreen.GetType() == typeof(T))
            {
                screen = (T)_currentScreen;
                return true;
            }

            screen = null;
            return false;
        }

        #endregion


        #region ___ POPUP ___

        public async Task<T> ShowPopup<T>(Action onShowed = null, Action onHidden = null) where T : Base_Popup
        {
            await EnsurePreloadCompleted();

            Base_Popup newPopup = GetInactivePopup<T>();

            if (newPopup == null)
            {
                Base_Popup prefab = await GetPopupPrefabAsync<T>();

                if (prefab == null)
                {
                    return null;
                }

                newPopup = Instantiate(prefab, _popupHolder);
            }

            RectTransform rectTransform = newPopup.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            newPopup.SetCallbacks(onShowed, onHidden);
            newPopup.transform.SetAsLastSibling();
            newPopup.OnShow();

            _activePopups.Add(newPopup);
            ActivePopupCount++;

            onPopupShown?.Invoke(newPopup);

            return newPopup as T;
        }

        private Base_Popup GetInactivePopup<T>() where T : Base_Popup
        {
            Type type = typeof(T);

            for (int i = _inactivePopups.Count - 1; i >= 0; i--)
            {
                Base_Popup popup = _inactivePopups[i];

                if (popup != null && popup.GetType() == type)
                {
                    _inactivePopups.RemoveAt(i);
                    popup.gameObject.SetActive(true);
                    return popup;
                }
            }

            return null;
        }

        public void HideTopPopup()
        {
            if (_activePopups.Count == 0)
            {
                return;
            }

            int lastIndex = _activePopups.Count - 1;
            Base_Popup topPopup = _activePopups[lastIndex];

            HidePopupInternal(topPopup);
        }

        public void HidePopup(Base_Popup targetPopup)
        {
            if (targetPopup == null || !_activePopups.Contains(targetPopup))
            {
                return;
            }

            HidePopupInternal(targetPopup);
        }

        public void HideAllPopups()
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                Base_Popup popup = _activePopups[i];
                HidePopupInternal(popup);
            }
        }

        public void HideAllPopups<T>() where T : Base_Popup
        {
            Type type = typeof(T);

            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                Base_Popup popup = _activePopups[i];

                if (popup != null && popup.GetType() == type)
                {
                    HidePopupInternal(popup);
                }
            }
        }

        private void HidePopupInternal(Base_Popup popup)
        {
            if (popup == null)
            {
                return;
            }

            if (!_activePopups.Remove(popup))
            {
                return;
            }

            ActivePopupCount = Mathf.Max(0, ActivePopupCount - 1);

            popup.OnHide(() =>
            {
                onPopupHidden?.Invoke(popup);

                if (popup.PoolAfterHide)
                {
                    _inactivePopups.Add(popup);
                }
                else
                {
                    Type type = popup.GetType();

                    Destroy(popup.gameObject);

                    TryReleasePopupPrefab(type);
                }
            });
        }

        public int GetActivePopupCount<T>() where T : Base_Popup
        {
            int count = 0;
            Type type = typeof(T);

            foreach (Base_Popup popup in _activePopups)
            {
                if (popup != null && popup.GetType() == type)
                {
                    count++;
                }
            }

            return count;
        }

        public List<T> GetAllActivePopups<T>() where T : Base_Popup
        {
            List<T> result = new();
            Type type = typeof(T);

            foreach (Base_Popup popup in _activePopups)
            {
                if (popup != null && popup.GetType() == type)
                {
                    result.Add(popup as T);
                }
            }

            return result;
        }

        public bool HasPopup<T>() where T : Base_Popup
        {
            Type type = typeof(T);

            foreach (Base_Popup popup in _activePopups)
            {
                if (popup != null && popup.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region ___ RELEASE ___

        private void TryReleaseScreenPrefab(Type type)
        {
            if (type == null)
            {
                return;
            }

            if (IsScreenInstanceAlive(type))
            {
                return;
            }

            if (_screenHandleDict.TryGetValue(type, out AsyncOperationHandle<GameObject> handle))
            {
                SafeRelease(handle);
                _screenHandleDict.Remove(type);
            }

            _screenPrefabDict.Remove(type);
        }

        private void TryReleasePopupPrefab(Type type)
        {
            if (type == null)
            {
                return;
            }

            if (IsPopupInstanceAlive(type))
            {
                return;
            }

            if (_popupHandleDict.TryGetValue(type, out AsyncOperationHandle<GameObject> handle))
            {
                SafeRelease(handle);
                _popupHandleDict.Remove(type);
            }

            _popupPrefabDict.Remove(type);
        }

        private bool IsScreenInstanceAlive(Type type)
        {
            if (_currentScreen != null && _currentScreen.GetType() == type)
            {
                return true;
            }

            foreach (Base_Screen screen in _inactiveScreens)
            {
                if (screen != null && screen.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPopupInstanceAlive(Type type)
        {
            foreach (Base_Popup popup in _activePopups)
            {
                if (popup != null && popup.GetType() == type)
                {
                    return true;
                }
            }

            foreach (Base_Popup popup in _inactivePopups)
            {
                if (popup != null && popup.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void SafeRelease(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseAllAddressables();
        }

        private void ReleaseAllAddressables()
        {
            foreach (AsyncOperationHandle<GameObject> handle in _screenHandleDict.Values)
            {
                SafeRelease(handle);
            }

            foreach (AsyncOperationHandle<GameObject> handle in _popupHandleDict.Values)
            {
                SafeRelease(handle);
            }

            _screenHandleDict.Clear();
            _popupHandleDict.Clear();

            _screenPrefabDict.Clear();
            _popupPrefabDict.Clear();
        }

        #endregion
    }
}