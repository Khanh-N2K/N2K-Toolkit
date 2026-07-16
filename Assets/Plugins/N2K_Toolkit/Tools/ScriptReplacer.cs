#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace N2K
{
    /// <summary>
    /// Editor tool to replace a MonoBehaviour script component on a GameObject with another script
    /// while preserving matching serialized field values and keeping external references intact.
    /// </summary>
    internal static class ScriptReplacer
    {
        [MenuItem("CONTEXT/MonoBehaviour/Replace Script")]
        private static void ReplaceScriptContext(MenuCommand menuCommand)
        {
            MonoBehaviour target = menuCommand.context as MonoBehaviour;
            if (target == null) return;

            ScriptReplacerWindow.ShowWindow(target);
        }
    }

    internal class ScriptReplacerWindow : EditorWindow
    {
        private MonoBehaviour m_TargetComponent;
        private MonoScript m_NewScript;

        // Analysis state
        private List<FieldDiff> m_PreservedFields = new List<FieldDiff>();
        private List<FieldDiff> m_MismatchedFields = new List<FieldDiff>();
        private List<FieldDiff> m_RemovedFields = new List<FieldDiff>();
        private List<FieldDiff> m_NewFields = new List<FieldDiff>();

        private Vector2 m_ScrollPosition;
        private bool m_ShowPreserved = true;
        private bool m_ShowMismatched = true;
        private bool m_ShowRemoved = true;
        private bool m_ShowNew = true;

        private class FieldDiff
        {
            public string Name;
            public string OldType;
            public string NewType;
        }

        public static void ShowWindow(MonoBehaviour target)
        {
            var window = GetWindow<ScriptReplacerWindow>(true, "Replace Script", true);
            window.m_TargetComponent = target;
            window.m_NewScript = null;
            window.minSize = new Vector2(350, 400);
            window.ClearAnalysis();
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (m_TargetComponent == null)
            {
                EditorGUILayout.HelpBox("Target component is null. The component may have been destroyed or selection lost.", MessageType.Warning);
                if (GUILayout.Button("Close Window")) Close();
                return;
            }

            // Header Section
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Target Component Info", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("GameObject", m_TargetComponent.gameObject, typeof(GameObject), true);
                    
                    MonoScript currentScript = MonoScript.FromMonoBehaviour(m_TargetComponent);
                    EditorGUILayout.ObjectField("Current Script", currentScript, typeof(MonoScript), false);
                }
            }

            // Input Section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Select Replacement Script", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            m_NewScript = (MonoScript)EditorGUILayout.ObjectField("New Script", m_NewScript, typeof(MonoScript), false);
            if (EditorGUI.EndChangeCheck())
            {
                AnalyzeFields();
            }

            // Validation & Errors
            string errorMsg = null;
            System.Type newScriptClass = null;

            if (m_NewScript != null)
            {
                newScriptClass = m_NewScript.GetClass();
                if (newScriptClass == null)
                {
                    errorMsg = "Selected script does not define a valid C# class.\nEnsure the filename matches the class name and compiles without errors.";
                }
                else if (!typeof(MonoBehaviour).IsAssignableFrom(newScriptClass))
                {
                    errorMsg = $"The class '{newScriptClass.Name}' does not inherit from MonoBehaviour.";
                }
                else if (newScriptClass.IsAbstract)
                {
                    errorMsg = $"The class '{newScriptClass.Name}' is abstract and cannot be instantiated.";
                }
                else if (newScriptClass == m_TargetComponent.GetType())
                {
                    errorMsg = "The new script is the same class as the current script.";
                }
            }

            if (errorMsg != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(errorMsg, MessageType.Error);
            }
            else if (m_NewScript != null && newScriptClass != null)
            {
                // Field Analysis UI
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Serialization Field Analysis", EditorStyles.boldLabel);

                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.ExpandHeight(true));
                
                DrawFieldGroup(ref m_ShowPreserved, "Preserved Fields", m_PreservedFields, new Color(0.2f, 0.7f, 0.2f), "✓");
                DrawFieldGroup(ref m_ShowMismatched, "Type Mismatch Fields (Will be lost/reset)", m_MismatchedFields, new Color(0.8f, 0.5f, 0.1f), "⚠");
                DrawFieldGroup(ref m_ShowRemoved, "Removed Fields (Will be lost)", m_RemovedFields, new Color(0.7f, 0.2f, 0.2f), "✗");
                DrawFieldGroup(ref m_ShowNew, "New Fields (Will get default values)", m_NewFields, new Color(0.2f, 0.5f, 0.8f), "+");

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("Drag and drop a new C# script asset above to see compatibility and run the replacement.", MessageType.Info);
            }

            // Action Buttons
            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(m_NewScript == null || errorMsg != null))
            {
                if (GUILayout.Button("Replace Script", GUILayout.Height(35)))
                {
                    if (EditorUtility.DisplayDialog("Replace Script?",
                        $"Are you sure you want to replace script on '{m_TargetComponent.gameObject.name}' with '{newScriptClass.Name}'?\n\nThis will preserve matching fields and scene references, and can be undone (Ctrl+Z).",
                        "Replace", "Cancel"))
                    {
                        PerformSwap(m_TargetComponent, m_NewScript);
                        m_TargetComponent = null; // Clear to prevent further GUI access to the destroyed object
                        Close();
                    }
                }
            }
            EditorGUILayout.Space(10);
        }

        private void DrawFieldGroup(ref bool foldoutState, string label, List<FieldDiff> list, Color color, string prefix)
        {
            if (list.Count == 0) return;

            var style = new GUIStyle(EditorStyles.foldout);
            style.normal.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.hover.textColor = color;

            foldoutState = EditorGUILayout.Foldout(foldoutState, $"{label} ({list.Count})", true, style);
            if (foldoutState)
            {
                EditorGUI.indentLevel++;
                foreach (var diff in list)
                {
                    string info;
                    if (!string.IsNullOrEmpty(diff.OldType) && !string.IsNullOrEmpty(diff.NewType) && diff.OldType != diff.NewType)
                    {
                        info = $"{diff.Name} ({diff.OldType} ➔ {diff.NewType})";
                    }
                    else
                    {
                        string typeStr = !string.IsNullOrEmpty(diff.NewType) ? diff.NewType : diff.OldType;
                        info = $"{diff.Name} ({typeStr})";
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var prefixStyle = new GUIStyle(EditorStyles.label);
                        prefixStyle.normal.textColor = color;
                        prefixStyle.fontStyle = FontStyle.Bold;
                        
                        EditorGUILayout.LabelField(prefix, prefixStyle, GUILayout.Width(15));
                        EditorGUILayout.LabelField(info);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(2);
        }

        private void ClearAnalysis()
        {
            m_PreservedFields.Clear();
            m_MismatchedFields.Clear();
            m_RemovedFields.Clear();
            m_NewFields.Clear();
        }

        private void AnalyzeFields()
        {
            ClearAnalysis();

            if (m_NewScript == null) return;

            System.Type oldType = m_TargetComponent.GetType();
            System.Type newType = m_NewScript.GetClass();

            if (newType == null || !typeof(MonoBehaviour).IsAssignableFrom(newType)) return;

            var oldFields = GetSerializedFields(oldType);
            var newFields = GetSerializedFields(newType);

            var oldDict = new Dictionary<string, FieldInfo>();
            foreach (var f in oldFields) oldDict[f.Name] = f;

            var newDict = new Dictionary<string, FieldInfo>();
            foreach (var f in newFields) newDict[f.Name] = f;

            // Find preserved and mismatched fields
            foreach (var oldPair in oldDict)
            {
                string fieldName = oldPair.Key;
                FieldInfo oldField = oldPair.Value;

                if (newDict.TryGetValue(fieldName, out FieldInfo newField))
                {
                    bool isCompatible = AreTypesCompatible(oldField.FieldType, newField.FieldType);
                    var diff = new FieldDiff
                    {
                        Name = fieldName,
                        OldType = GetFriendlyTypeName(oldField.FieldType),
                        NewType = GetFriendlyTypeName(newField.FieldType)
                    };

                    if (isCompatible)
                    {
                        m_PreservedFields.Add(diff);
                    }
                    else
                    {
                        m_MismatchedFields.Add(diff);
                    }
                }
                else
                {
                    m_RemovedFields.Add(new FieldDiff
                    {
                        Name = fieldName,
                        OldType = GetFriendlyTypeName(oldField.FieldType)
                    });
                }
            }

            // Find new fields
            foreach (var newPair in newDict)
            {
                string fieldName = newPair.Key;
                FieldInfo newField = newPair.Value;

                if (!oldDict.ContainsKey(fieldName))
                {
                    m_NewFields.Add(new FieldDiff
                    {
                        Name = fieldName,
                        NewType = GetFriendlyTypeName(newField.FieldType)
                    });
                }
            }
        }

        private static List<FieldInfo> GetSerializedFields(System.Type type)
        {
            var fields = new List<FieldInfo>();
            var currentType = type;

            while (currentType != null &&
                   currentType != typeof(MonoBehaviour) &&
                   currentType != typeof(Behaviour) &&
                   currentType != typeof(Component) &&
                   currentType != typeof(UnityEngine.Object))
            {
                var localFields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in localFields)
                {
                    if (IsSerialized(field))
                    {
                        if (!fields.Exists(f => f.Name == field.Name))
                        {
                            fields.Add(field);
                        }
                    }
                }
                currentType = currentType.BaseType;
            }
            return fields;
        }

        private static bool IsSerialized(FieldInfo field)
        {
            if (field.IsInitOnly) return false;
            if (field.IsLiteral) return false;
            if (System.Attribute.IsDefined(field, typeof(System.NonSerializedAttribute))) return false;
            if (field.IsPublic) return true;
            if (System.Attribute.IsDefined(field, typeof(SerializeField))) return true;
            if (System.Attribute.IsDefined(field, typeof(SerializeReference))) return true;
            return false;
        }

        private static bool AreTypesCompatible(System.Type t1, System.Type t2)
        {
            if (t1 == t2) return true;
            if (t2.IsAssignableFrom(t1)) return true;
            
            // Basic numeric compatibility (Unity can often cast them when serializing/deserializing, 
            // but we check if they are both numeric primitives to flag as compatible)
            if (t1.IsPrimitive && t2.IsPrimitive)
            {
                if ((t1 == typeof(int) || t1 == typeof(float) || t1 == typeof(double) || t1 == typeof(long) || t1 == typeof(short) || t1 == typeof(byte)) &&
                    (t2 == typeof(int) || t2 == typeof(float) || t2 == typeof(double) || t2 == typeof(long) || t2 == typeof(short) || t2 == typeof(byte)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetFriendlyTypeName(System.Type type)
        {
            if (type.IsGenericType)
            {
                var name = type.Name.Split('`')[0];
                var args = new List<string>();
                foreach (var arg in type.GetGenericArguments())
                {
                    args.Add(GetFriendlyTypeName(arg));
                }
                return $"{name}<{string.Join(", ", args)}>";
            }
            return type.Name;
        }

        private static void PerformSwap(MonoBehaviour target, MonoScript newScript)
        {
            if (target == null || newScript == null) return;

            GameObject go = target.gameObject;
            System.Type newScriptClass = newScript.GetClass();
            if (newScriptClass == null) return;

            // 1. Use RegisterCompleteObjectUndo as required when type tree changes
            Undo.RegisterCompleteObjectUndo(target, "Replace Script");
            Undo.RegisterCompleteObjectUndo(go, "Replace Script");

            // 2. Modify the m_Script reference using SerializedObject
            SerializedObject so = new SerializedObject(target);
            SerializedProperty scriptProperty = so.FindProperty("m_Script");

            if (scriptProperty != null)
            {
                so.Update();
                scriptProperty.objectReferenceValue = newScript;
                so.ApplyModifiedProperties();

                // After ApplyModifiedProperties, the old component is destroyed and replaced
                // We must retrieve the newly created component instance
                MonoBehaviour newComponent = go.GetComponent(newScriptClass) as MonoBehaviour;

                if (newComponent != null)
                {
                    // 3. Mark the modified component as dirty
                    EditorUtility.SetDirty(newComponent);
                }
                
                // If it is in a scene, mark scene dirty
                if (go.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                }

                Debug.Log($"[ScriptReplacer] Successfully swapped script component on GameObject '{go.name}' to '{newScriptClass.Name}'. Serialized data migrated.");

                // 4. Force Unity's UI and Active Editor to redraw
                ActiveEditorTracker.sharedTracker.ForceRebuild();

                // Temporarily toggle selection to force the inspector to re-inspect the new type structure
                Selection.activeGameObject = null;
                EditorApplication.delayCall += () =>
                {
                    if (go != null)
                    {
                        Selection.activeGameObject = go;
                    }
                };
            }
            else
            {
                Debug.LogError("[ScriptReplacer] Failed to find the m_Script property. Script replacement cancelled.");
            }
        }
    }
}
#endif
