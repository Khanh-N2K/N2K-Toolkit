#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace N2K
{
    [InitializeOnLoad]
    internal static class PinnedAssetsProjectOverlay
    {
        private const string PREFS_KEY = "UserPinnedAssets_UIToolkit_GUIDs";
        private static double lastUpdateTime;

        static PinnedAssetsProjectOverlay()
        {
            EditorApplication.projectWindowItemOnGUI += DrawPinIcon;
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

        private static void DrawPinIcon(string guid, Rect selectionRect)
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

            var pinned = PinnedAssetsWindow.GetPinnedGUIDs();
            bool isPinned = pinned.Contains(guid);

            bool isHovered = selectionRect.Contains(Event.current.mousePosition);
            if (!isPinned && !isHovered) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            float buttonSize = 16f;
            Rect pinRect;

            if (selectionRect.height > 20f)
            {
                // Grid view (large thumbnails)
                pinRect = new Rect(selectionRect.xMax - buttonSize - 4f, selectionRect.y + 4f, buttonSize, buttonSize);
            }
            else
            {
                // List view (single row)
                pinRect = new Rect(selectionRect.xMax - buttonSize - 8f, selectionRect.y + (selectionRect.height - buttonSize) / 2f, buttonSize, buttonSize);
            }

            // Draw a solid star character (★) if pinned, or outline star character (☆) if unpinned
            string starChar = isPinned ? "★" : "☆";

            GUIStyle buttonStyle = new GUIStyle(GUIStyle.none);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 14;

            if (GUI.Button(pinRect, starChar, buttonStyle))
            {
                if (isPinned)
                {
                    pinned.Remove(guid);
                }
                else
                {
                    pinned.Add(guid);
                }

                EditorPrefs.SetString(PREFS_KEY, string.Join(",", pinned));

                var windows = Resources.FindObjectsOfTypeAll<PinnedAssetsWindow>();
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