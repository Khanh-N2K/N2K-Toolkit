#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace N2K
{
    [InitializeOnLoad]
    internal static class QuickAssetsProjectOverlay
    {
        private static string PREFS_KEY => "UserQuickAssets_" + Application.dataPath.Replace('/', '_').Replace(':', '_');
        private static double lastUpdateTime;

        static QuickAssetsProjectOverlay()
        {
            EditorApplication.projectWindowItemOnGUI += DrawQuickIcon;
            EditorApplication.update += UpdateRepaint;
        }

        private static void UpdateRepaint()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastUpdateTime < 0.2) return;
            lastUpdateTime = currentTime;

            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            if (windows != null)
            {
                foreach (var window in windows)
                {
                    if (window != null && window.GetType().Name == "ProjectBrowser")
                    {
                        if (!window.wantsMouseMove)
                        {
                            window.wantsMouseMove = true;
                        }
                    }
                }
            }
        }

        private static void DrawQuickIcon(string guid, Rect selectionRect)
        {
            if (string.IsNullOrEmpty(guid)) return;

            // Trigger repaint on MouseMove so hover state updates immediately with no delay
            if (Event.current.type == EventType.MouseMove)
            {
                if (EditorWindow.mouseOverWindow != null)
                {
                    EditorWindow.mouseOverWindow.Repaint();
                }
            }

            var quickAssets = QuickAssetsWindow.GetquickGUIDs();
            bool isQuick = quickAssets.Contains(guid);

            bool isHovered = selectionRect.Contains(Event.current.mousePosition);
            if (!isQuick && !isHovered) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            float buttonSize = 16f;
            Rect quickRect;

            if (selectionRect.height > 20f)
            {
                // Grid view (large thumbnails)
                quickRect = new Rect(selectionRect.xMax - buttonSize - 4f, selectionRect.y + 4f, buttonSize, buttonSize);
            }
            else
            {
                // List view (single row)
                quickRect = new Rect(selectionRect.xMax - buttonSize - 8f, selectionRect.y + (selectionRect.height - buttonSize) / 2f, buttonSize, buttonSize);
            }

            // Draw a solid star character (★) if in quick assets, or outline star character (☆) if not in quick assets
            string starChar = isQuick ? "★" : "☆";

            GUIStyle buttonStyle = new GUIStyle(GUIStyle.none);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 14;

            if (GUI.Button(quickRect, starChar, buttonStyle))
            {
                if (isQuick)
                {
                    quickAssets.Remove(guid);
                }
                else
                {
                    quickAssets.Add(guid);
                }

                EditorPrefs.SetString(PREFS_KEY, string.Join(",", quickAssets));

                var windows = Resources.FindObjectsOfTypeAll<QuickAssetsWindow>();
                if (windows != null && windows.Length > 0)
                {
                    foreach (var window in windows)
                    {
                        window.RefreshCache();
                        window.RefreshUI();
                    }
                }

                EditorApplication.RepaintProjectWindow();
                Event.current.Use();
            }
        }
    }
}

#endif