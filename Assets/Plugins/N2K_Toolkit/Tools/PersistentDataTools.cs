#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace N2K
{
    internal static class PersistentDataTools
    {
        [MenuItem("Tools/N2K Toolkit/Persistent Data/Clear All Data")]
        internal static void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            string path = Application.persistentDataPath;
            if (System.IO.Directory.Exists(path))
            {
                var dir = new System.IO.DirectoryInfo(path);
                foreach (System.IO.FileInfo file in dir.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[PersistentDataTools] Could not delete file {file.Name}: {e.Message}");
                    }
                }
                foreach (System.IO.DirectoryInfo subDir in dir.GetDirectories())
                {
                    try
                    {
                        subDir.Delete(true);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[PersistentDataTools] Could not delete directory {subDir.Name}: {e.Message}");
                    }
                }
            }

            Debug.Log("🧹 Cleared PlayerPrefs and all files in persistent data path.");
        }

        [MenuItem("Tools/N2K Toolkit/Persistent Data/Reveal In File Explorer")]
        internal static void RevealInExplorer()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
#endif