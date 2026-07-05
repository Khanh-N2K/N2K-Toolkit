#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace N2K
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private static readonly float LINE = EditorGUIUtility.singleLineHeight;
        private const float PAD = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool isReadOnly = HasReadOnlyAttribute();

            using (new EditorGUI.DisabledScope(isReadOnly))
            {
                EditorGUI.BeginProperty(position, label, property);

                if (property.propertyType != SerializedPropertyType.ManagedReference)
                {
                    DrawFallbackObjectField(position, property, label);
                    EditorGUI.EndProperty();
                    return;
                }

                bool hasChildren = HasChildren(property);

                Rect header = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    EditorGUIUtility.singleLineHeight);

                Rect dropdownRect;

                if (hasChildren)
                {
                    Rect foldoutRect = new Rect(
                        header.x,
                        header.y,
                        EditorGUIUtility.labelWidth,
                        header.height);

                    float controlX = header.x + EditorGUIUtility.labelWidth + 2f;

                    dropdownRect = new Rect(
                        controlX,
                        header.y,
                        header.xMax - controlX,
                        header.height);

                    property.isExpanded = EditorGUI.Foldout(
                        foldoutRect,
                        property.isExpanded,
                        label,
                        true);
                }
                else
                {
                    dropdownRect = EditorGUI.PrefixLabel(header, label);
                }

                DrawDropdown(dropdownRect, property);

                if (hasChildren && property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawChildren(position, property);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndProperty();
            }
        }

        private void DrawFallbackObjectField(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                Type objectType = fieldInfo != null ? fieldInfo.FieldType : typeof(UnityEngine.Object);

                if (!typeof(UnityEngine.Object).IsAssignableFrom(objectType))
                    objectType = typeof(UnityEngine.Object);

                EditorGUI.BeginChangeCheck();

                UnityEngine.Object value = EditorGUI.ObjectField(
                    position,
                    label,
                    property.objectReferenceValue,
                    objectType,
                    true);

                if (EditorGUI.EndChangeCheck())
                {
                    property.objectReferenceValue = value;
                }

                return;
            }

            EditorGUI.HelpBox(
                position,
                "[SubclassSelector] only works with [SerializeReference]. For Unity Object fields, use [SerializeField] instead.",
                (UnityEditor.MessageType)MessageType.Warning);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded && HasChildren(property))
            {
                SerializedProperty it = property.Copy();
                bool enterChildren = true;

                while (it.NextVisible(enterChildren))
                {
                    if (it.depth <= property.depth)
                        break;

                    height += EditorGUI.GetPropertyHeight(it, true) + PAD;
                    enterChildren = false;
                }
            }

            return height;
        }

        private void DrawDropdown(Rect rect, SerializedProperty property)
        {
            Type baseType = GetBaseType(property);
            if (baseType == null)
                return;

            Type[] types = SubclassTypeCache.GetDerivedTypes(baseType);

            string currentLabel = property.managedReferenceValue == null
                ? "None"
                : ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name);

            if (EditorGUI.DropdownButton(rect, new GUIContent(currentLabel), FocusType.Keyboard, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("None"),
                    property.managedReferenceValue == null,
                    () =>
                    {
                        property.managedReferenceValue = null;
                        property.isExpanded = false;
                        property.serializedObject.ApplyModifiedProperties();
                    });

                foreach (Type type in types)
                {
                    bool selected = property.managedReferenceValue?.GetType() == type;

                    menu.AddItem(
                        new GUIContent(ObjectNames.NicifyVariableName(type.Name)),
                        selected,
                        () =>
                        {
                            property.managedReferenceValue = Activator.CreateInstance(type);
                            property.serializedObject.ApplyModifiedProperties();

                            property.isExpanded = HasChildren(property);
                            property.serializedObject.ApplyModifiedProperties();
                        });
                }

                menu.ShowAsContext();
            }
        }

        private void DrawChildren(Rect position, SerializedProperty property)
        {
            SerializedProperty it = property.Copy();
            bool enterChildren = true;
            float y = position.y + LINE + PAD;

            while (it.NextVisible(enterChildren))
            {
                if (it.depth <= property.depth)
                    break;

                float h = EditorGUI.GetPropertyHeight(it, true);

                Rect childRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    h);

                EditorGUI.PropertyField(childRect, it, true);

                y += h + PAD;
                enterChildren = false;
            }
        }

        private static bool HasChildren(SerializedProperty property)
        {
            if (property.managedReferenceValue == null)
                return false;

            SerializedProperty it = property.Copy();
            bool enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                if (it.depth <= property.depth)
                    break;

                return true;
            }

            return false;
        }

        private static Type GetBaseType(SerializedProperty property)
        {
            string typename = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(typename))
                return null;

            string[] split = typename.Split(' ');
            return Type.GetType($"{split[1]}, {split[0]}");
        }

        private bool HasReadOnlyAttribute()
        {
            return fieldInfo != null &&
                   fieldInfo.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length > 0;
        }
    }
}
#endif