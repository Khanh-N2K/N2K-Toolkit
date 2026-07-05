#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// This will save the object as an independent prefab, from both runtime or editor, 
/// but also save any instanced materials and meshes it uses as separate assets, and re-link them in the prefab. 
/// This is useful for quickly saving objects that were created at runtime or have unique materials/meshes without polluting the project with unnecessary assets.
/// </summary>
namespace N2K
{
    internal static class InstancedPrefabSaver
    {
        #region MENU ITEM ________________________________________________________________ 
        [MenuItem("GameObject/Save Selected As INSTANCED Prefab", false, 0)]
        internal static void SaveSelected()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("No GameObject selected!");
                return;
            }
            SaveWithInstancedMaterials(Selection.activeGameObject, "Assets");
        }

        [MenuItem("GameObject/Save Selected As INSTANCED Prefab", true)]
        private static bool ValidateSaveSelected()
        {
            return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject);
        }
        #endregion ________________________________________________________________

        private static void SaveWithInstancedMaterials(GameObject go, string baseFolder)
        {
            string objectFolder = $"{baseFolder}/{go.name}";
            string materialsFolder = $"{objectFolder}/_Materials";
            string meshesFolder = $"{objectFolder}/_Meshes";

            // === Ensure Folder Structure ===
            if (!AssetDatabase.IsValidFolder(objectFolder))
                AssetDatabase.CreateFolder(baseFolder, go.name);

            if (!AssetDatabase.IsValidFolder(materialsFolder))
                AssetDatabase.CreateFolder(objectFolder, "_Materials");

            if (!AssetDatabase.IsValidFolder(meshesFolder))
                AssetDatabase.CreateFolder(objectFolder, "_Meshes");

            // === Clone so we don't touch scene object ===
            GameObject copy = Object.Instantiate(go);
            copy.name = go.name;

            SaveInstancedMeshes(copy, meshesFolder);
            SaveInstancedMaterials(copy, materialsFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // === Save prefab ===
            string prefabPath = $"{objectFolder}/{go.name}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
            Object.DestroyImmediate(copy);

            Debug.Log($"[GameObjPrefabSaver] Saved prefab + materials + meshes → {objectFolder}");
        }

        static void SaveInstancedMeshes(GameObject root, string meshesFolder)
        {
            int meshIndex = 0;

            // MeshFilter (static meshes)
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;

                bool isInstanced = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh));

                if (!isInstanced) continue;

                string safeName = mf.name.Replace("/", "_");
                string meshPath = $"{meshesFolder}/{safeName}_Mesh{meshIndex}.asset";
                meshIndex++;

                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
                    AssetDatabase.DeleteAsset(meshPath);

                Mesh newMesh = Object.Instantiate(mesh);
                newMesh.name = mesh.name;

                AssetDatabase.CreateAsset(newMesh, meshPath);
                mf.sharedMesh = newMesh;

                Debug.Log($"Saved Mesh → {meshPath}");
            }

            // SkinnedMeshRenderer (skinned meshes)
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = smr.sharedMesh;
                if (mesh == null) continue;

                bool isInstanced = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh));

                if (!isInstanced) continue;

                string safeName = smr.name.Replace("/", "_");
                string meshPath = $"{meshesFolder}/{safeName}_SkinnedMesh{meshIndex}.asset";
                meshIndex++;

                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
                    AssetDatabase.DeleteAsset(meshPath);

                Mesh newMesh = Object.Instantiate(mesh);
                newMesh.name = mesh.name;

                AssetDatabase.CreateAsset(newMesh, meshPath);
                smr.sharedMesh = newMesh;

                Debug.Log($"Saved SkinnedMesh → {meshPath}");
            }
        }

        static void SaveInstancedMaterials(GameObject root, string materialsFolder)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            int globalMatIndex = 0;

            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null) continue;

                    bool isInstanced =
                        string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)) ||
                        AssetDatabase.IsSubAsset(mat);

                    if (!isInstanced) continue;

                    string safeName = r.name.Replace("/", "_");
                    string matPath = $"{materialsFolder}/{safeName}_Slot{i}_Mat{globalMatIndex}.mat";
                    globalMatIndex++;

                    if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                        AssetDatabase.DeleteAsset(matPath);

                    Material newMat = Object.Instantiate(mat);

                    // Ensure real shader asset
                    string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                    Shader realShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                    newMat.shader = realShader != null ? realShader : Shader.Find(mat.shader.name);

                    AssetDatabase.CreateAsset(newMat, matPath);
                    mats[i] = newMat;

                    Debug.Log($"Saved Material → {matPath}");
                }

                r.sharedMaterials = mats;
            }
        }
    }
}
#endif
