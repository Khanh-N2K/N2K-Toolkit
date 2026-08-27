#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace N2K
{
    internal static class N2K_Toolbar
    {
        [MainToolbarElement("N2K Toolkit", defaultDockPosition = MainToolbarDockPosition.Middle)]
        internal static MainToolbarElement MenuButton()
        {
            var content = new MainToolbarContent("N2K Toolkit", "Open N2K Toolkit");
            return new MainToolbarDropdown(content, ShowPopup);
        }

        private static void ShowPopup(Rect rect)
        {
            PopupWindow.Show(rect, new N2KToolbarPopup());
        }
    }

    internal class N2KToolbarPopup : PopupWindowContent
    {
        private const float Padding = 10f;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(250, 120); // Initial size, will be dynamically auto-adjusted in OnGUI
        }

        public override void OnGUI(Rect rect)
        {
            DrawTitle("Persistent Data");

            DrawLargeButton("Clear All Data", true, () =>
            {
                PersistentDataTools.ClearAllData();
                editorWindow.Close();
            });

            DrawLargeButton("Reveal In File Explorer", true, () =>
            {
                PersistentDataTools.RevealInExplorer();
                editorWindow.Close();
            });

            DrawTitleButton("Quick Assets", true, () =>
            {
                QuickAssetsWindow.ShowWindow();
                editorWindow.Close();
            });

            DrawTitleButton("Source Code Metrics", true, () =>
            {
                SourceCodeMetricWindow.ShowWindow();
                editorWindow.Close();
            });

            // Dynamically auto-adjust window height based on GUI elements size
            if (editorWindow != null && Event.current.type == EventType.Repaint)
            {
                float calculatedHeight = GUILayoutUtility.GetLastRect().yMax + Padding;
                if (Mathf.Abs(editorWindow.position.height - calculatedHeight) > 2f)
                {
                    editorWindow.minSize = new Vector2(250, calculatedHeight);
                    editorWindow.maxSize = new Vector2(250, calculatedHeight);
                }
            }
        }

        private static void DrawTitle(string text)
        {
            GUILayout.Label(text, EditorStyles.boldLabel);
        }

        private void DrawLargeButton(string text, bool enabled, System.Action onClick)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                Rect buttonRect = GUILayoutUtility.GetRect(
                    new GUIContent(text),
                    EditorStyles.toolbarButton,
                    GUILayout.Height(20),
                    GUILayout.ExpandWidth(true)
                );

                if (GUI.Button(buttonRect, text, EditorStyles.toolbarButton))
                {
                    onClick?.Invoke();
                }
            }
        }

        private static GUIStyle titleButtonStyle;

        private static void DrawTitleButton(string text, bool enabled, System.Action onClick)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (titleButtonStyle == null)
                {
                    titleButtonStyle = new GUIStyle(EditorStyles.boldLabel);
                    titleButtonStyle.hover.textColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.3f, 0.67f, 0.98f) 
                        : new Color(0.0f, 0.47f, 0.83f);
                    titleButtonStyle.active.textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.18f, 0.53f, 0.88f)
                        : new Color(0.0f, 0.33f, 0.63f);
                }

                if (GUILayout.Button(text, titleButtonStyle))
                {
                    onClick?.Invoke();
                }
            }
        }
    }
}
#endif