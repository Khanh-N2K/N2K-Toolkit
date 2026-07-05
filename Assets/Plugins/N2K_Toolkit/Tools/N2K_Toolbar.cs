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
            return new Vector2(250, 120);
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

            GUILayout.Space(Padding);
            DrawTitle("Source Code Metrics");

            DrawLargeButton("Open Metric Window", true, () =>
            {
                SourceCodeMetricWindow.ShowWindow();
                editorWindow.Close();
            });
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
    }
}
#endif