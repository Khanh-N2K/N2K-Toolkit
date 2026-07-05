#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace N2K
{
    internal class SourceCodeMetricWindow : EditorWindow
    {
        // ── Serialised state ─────────────────────────────────────────────────
        [SerializeField]
        private List<string> foldersToIgnore = new List<string> { "Editor", "Plugins", "ThirdParty" };

        // ── Results ───────────────────────────────────────────────────────────
        private bool  _hasResults    = false;
        private int   _codeLines     = 0;
        private int   _commentLines  = 0;
        private int   _blankLines    = 0;
        private int   _totalLines    = 0;
        private int   _classCount    = 0;
        private int   _namespaceCount= 0;

        // ── Scroll & foldout ─────────────────────────────────────────────────
        private Vector2 _scroll;
        private bool    _foldoutOpen = false;

        // ── Styles (created lazily to avoid constructor issues) ───────────────
        private GUIStyle _headerStyle;
        private GUIStyle _rowEvenStyle;
        private GUIStyle _rowOddStyle;

        // ── Menu entry ────────────────────────────────────────────────────────
        [MenuItem("Tools/N2K Toolkit/Source Code Metric")]
        internal static void ShowWindow()
        {
            var window = GetWindow<SourceCodeMetricWindow>("Source Code Metric");
            window.minSize = new Vector2(340, 260);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Space(8);

            // ─── Ignored Folders ──────────────────────────────────────────────
            _foldoutOpen = EditorGUILayout.Foldout(_foldoutOpen, "Folders to Ignore (relative to Assets)", true);
            if (_foldoutOpen)
            {
                EditorGUI.indentLevel++;
                SerializedObject so = new SerializedObject(this);
                SerializedProperty prop = so.FindProperty("foldersToIgnore");
                EditorGUILayout.PropertyField(prop, true);
                so.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // ─── Run button ───────────────────────────────────────────────────
            if (GUILayout.Button("Run Analysis", GUILayout.Height(28)))
            {
                RunAnalysis();
            }

            GUILayout.Space(10);

            // ─── Results ──────────────────────────────────────────────────────
            if (_hasResults)
            {
                DrawResultsTable();
            }
            else
            {
                EditorGUILayout.HelpBox("Press \"Run Analysis\" to scan your C# scripts.", UnityEditor.MessageType.Info);
            }

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        // ── Analysis ──────────────────────────────────────────────────────────
        private void RunAnalysis()
        {
            var ignore = new List<string>(foldersToIgnore);

            CountLinesOfCode.LineCountResult loc = CountLinesOfCode.CountLines(ignore);
            _codeLines      = loc.CodeLines;
            _commentLines   = loc.CommentLines;
            _blankLines     = loc.BlankLines;
            _totalLines     = loc.TotalLines;
            _classCount     = CountClassesTool.CountClasses(ignore);
            _namespaceCount = CountNamespacesTool.CountNamespaces(ignore);
            _hasResults     = true;

            Repaint();
        }

        // ── Drawing ───────────────────────────────────────────────────────────
        private void DrawResultsTable()
        {
            GUILayout.Label("Results", _headerStyle);
            GUILayout.Space(4);

            DrawRow("Code Lines",     _codeLines.ToString(),     true);
            DrawRow("Comment Lines",  _commentLines.ToString(),  false);
            DrawRow("Blank Lines",    _blankLines.ToString(),    true);
            DrawRow("Total Lines",    _totalLines.ToString(),    false);

            GUILayout.Space(6);

            DrawRow("Classes",        _classCount.ToString(),     true);
            DrawRow("Namespaces",     _namespaceCount.ToString(), false);
        }

        private void DrawRow(string label, string value, bool even)
        {
            GUIStyle style = even ? _rowEvenStyle : _rowOddStyle;

            Rect rowRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            GUI.Box(rowRect, GUIContent.none, style);

            Rect labelRect = new Rect(rowRect.x + 8,  rowRect.y + 3, rowRect.width * 0.65f, rowRect.height);
            Rect valueRect = new Rect(rowRect.x + rowRect.width * 0.65f, rowRect.y + 3, rowRect.width * 0.32f, rowRect.height);

            GUI.Label(labelRect, label);
            GUI.Label(valueRect, value, EditorStyles.boldLabel);
        }

        // ── Style init ────────────────────────────────────────────────────────
        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin   = new RectOffset(4, 4, 4, 4)
            };

            _rowEvenStyle = new GUIStyle();
            _rowEvenStyle.normal.background = MakeTex(1, 1, new Color(0.22f, 0.22f, 0.22f, 0.4f));

            _rowOddStyle = new GUIStyle();
            _rowOddStyle.normal.background = MakeTex(1, 1, new Color(0.18f, 0.18f, 0.18f, 0.2f));
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }
    }
}
#endif
