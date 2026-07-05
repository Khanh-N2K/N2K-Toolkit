#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace N2K
{
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(12);

            if (GUILayout.Button("Setup Addressable For Prefabs", GUILayout.Height(32)))
            {
                SetupAddressablesForPrefabs();
            }
        }

        private void SetupAddressablesForPrefabs()
        {
            serializedObject.Update();

            Object screenFolder = serializedObject.FindProperty("_screenPrefabFolder").objectReferenceValue;
            Object popupFolder = serializedObject.FindProperty("_popupPrefabFolder").objectReferenceValue;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogError("Addressable settings not found. Open Window > Asset Management > Addressables > Groups first and create Addressables settings.");
                return;
            }

            AddressableAssetGroup targetGroup = settings.DefaultGroup;

            if (targetGroup == null)
            {
                Debug.LogError("Addressables Default Group not found.");
                return;
            }

            int screenCount = SetupFolder<Base_Screen>(screenFolder, settings, targetGroup, "Screen");
            int popupCount = SetupFolder<Base_Popup>(popupFolder, settings, targetGroup, "Popup");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Setup Addressables finished. Screens: {screenCount}, Popups: {popupCount}");
        }

        private int SetupFolder<TBase>(
            Object folderObject,
            AddressableAssetSettings settings,
            AddressableAssetGroup targetGroup,
            string uiTypeName)
            where TBase : Component
        {
            if (folderObject == null)
            {
                Debug.LogWarning($"{uiTypeName} prefab folder is not assigned.");
                return 0;
            }

            string folderPath = AssetDatabase.GetAssetPath(folderObject);

            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"{uiTypeName} prefab folder is invalid: {folderPath}");
                return 0;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            int setupCount = 0;
            HashSet<string> usedAddresses = new();

            foreach (string guid in prefabGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    continue;
                }

                TBase baseComponent = prefab.GetComponent<TBase>();

                if (baseComponent == null)
                {
                    continue;
                }

                string address = baseComponent.GetType().Name;

                if (!usedAddresses.Add(address))
                {
                    Debug.LogError($"Duplicate {uiTypeName} Addressable address found in folder scan: {address}. Asset: {assetPath}");
                    continue;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, targetGroup, false, true);

                if (entry == null)
                {
                    Debug.LogError($"Failed to create Addressable entry for: {assetPath}");
                    continue;
                }

                entry.SetAddress(address);

                EditorUtility.SetDirty(prefab);

                setupCount++;

                Debug.Log($"{uiTypeName} Addressable setup: {assetPath} -> {address}");
            }

            return setupCount;
        }
    }
}
#endif