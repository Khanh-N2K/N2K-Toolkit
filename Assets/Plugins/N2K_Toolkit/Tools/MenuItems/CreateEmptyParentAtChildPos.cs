#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace N2K
{
    internal static class CreateEmptyParentAtChildPos
    {
        [MenuItem("GameObject/Create Empty Parent (At Child Pos)", false, 0)]
        static void CreateParentAtChildPosition(MenuCommand menuCommand)
        {
            // Unity's Hierarchy context menu calls this method for EVERY selected object.
            // By checking against Selection.activeGameObject, we ensure the logic only runs ONCE.
            if (menuCommand.context != null && Selection.activeGameObject != null && menuCommand.context != Selection.activeGameObject)
                return;

            // Use TopLevel to avoid tearing children away from selected parents if both are selected
            Transform[] selection = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);

            if (selection.Length == 0)
                return;

            // Calculate average world position
            Vector3 center = Vector3.zero;
            foreach (var t in selection)
                center += t.position;
            center /= selection.Length;

            // Create parent
            GameObject parent = new GameObject("GameObject");
            Undo.RegisterCreatedObjectUndo(parent, "Create Empty Parent");
            parent.transform.position = center;
            parent.transform.rotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;

            // Preserve hierarchy level based on the first selected object
            if (selection[0].parent != null)
            {
                parent.transform.SetParent(selection[0].parent, true);
                parent.transform.SetSiblingIndex(selection[0].GetSiblingIndex());
            }

            // Re-parent selected objects
            foreach (var t in selection)
            {
                Undo.SetTransformParent(t, parent.transform, "Reparent To Empty");
            }

            Selection.activeGameObject = parent;
            ExpandInHierarchy(parent);
        }

        [MenuItem("GameObject/Create Empty Parent (At Child Pos)", true)]
        static bool ValidateCreateParent()
        {
            if (Selection.transforms.Length == 0) return false;

            // Don't show the button if the selections not having same parents
            Transform parent = Selection.transforms[0].parent;
            for (int i = 1; i < Selection.transforms.Length; i++)
            {
                if (parent != Selection.transforms[i].parent) return false;
            }

            return true;
        }

        static void ExpandInHierarchy(GameObject go)
        {
            // Get Unity's internal SceneHierarchyWindow type
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (type == null) return;

            // Get the currently open Hierarchy window
            var window = EditorWindow.GetWindow(type);

            // Find the internal method "SetExpanded(int, bool)". 
            // We specify the parameters (int, bool) to avoid AmbiguousMatchExceptions in newer Unity versions.
            var method = type.GetMethod("SetExpanded",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new System.Type[] { typeof(int), typeof(bool) },
                null);

            if (method != null)
            {
                // Invoke the method: SetExpanded(instanceID, true)
                method.Invoke(window, new object[] { go.GetInstanceID(), true });
            }
        }
    }
}
#endif