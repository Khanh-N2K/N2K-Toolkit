#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace N2K
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class FoldOutEditor : Editor
    {
        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private Dictionary<string, Attribute> _attributeCache = new Dictionary<string, Attribute>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            bool isInsideFoldout = false;
            bool drawCurrentGroup = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Always draw the default script field at the top normally
                if (iterator.name == "m_Script")
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(iterator);
                    GUI.enabled = true;
                    continue;
                }

                // 1. Check if we need to START a new foldout
                FoldOutAttribute foldoutAttr = GetAttribute<FoldOutAttribute>(iterator);
                if (foldoutAttr != null)
                {
                    isInsideFoldout = true;
                    string foldName = foldoutAttr.Name;

                    if (!_foldouts.ContainsKey(foldName))
                        _foldouts[foldName] = false;

                    EditorGUILayout.Space();

                    // Draw the foldout header
                    _foldouts[foldName] = EditorGUILayout.Foldout(
                        _foldouts[foldName],
                        foldName,
                        true,
                        EditorStyles.foldout
                    );

                    drawCurrentGroup = _foldouts[foldName];
                }

                // 2. Draw the actual variable (it will draw using the current foldout state)
                if (!isInsideFoldout || drawCurrentGroup)
                {
                    if (isInsideFoldout) EditorGUI.indentLevel++; // Indent fields inside the foldout
                    EditorGUILayout.PropertyField(iterator, true);
                    if (isInsideFoldout) EditorGUI.indentLevel--; // Reset indent
                }

                // 3. Check if we need to STOP folding (applied AFTER drawing the current variable)
                if (HasAttribute<EndFoldOutAttribute>(iterator))
                {
                    isInsideFoldout = false;
                    drawCurrentGroup = true; // Reset draw state for the unguouped fields following this
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Helper to get attributes and cache them for perfect performance
        private T GetAttribute<T>(SerializedProperty prop) where T : Attribute
        {
            string key = prop.name + "_" + typeof(T).Name;

            if (_attributeCache.ContainsKey(key))
                return _attributeCache[key] as T;

            Type type = serializedObject.targetObject.GetType();
            FieldInfo field = null;

            while (type != null && field == null)
            {
                field = type.GetField(prop.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            T attr = null;
            if (field != null)
            {
                var attributes = field.GetCustomAttributes(typeof(T), true);
                if (attributes.Length > 0) attr = attributes[0] as T;
            }

            _attributeCache[key] = attr;
            return attr;
        }

        private bool HasAttribute<T>(SerializedProperty prop) where T : Attribute
        {
            return GetAttribute<T>(prop) != null;
        }
    }
}
#endif