#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace N2K
{
    internal class QuickAssetsWindow : EditorWindow
    {
        private static string PREFS_KEY => "UserQuickAssets_" + Application.dataPath.Replace('/', '_').Replace(':', '_');
        private static string PREFS_MODE_KEY => "UserQuickAssets_ViewMode_" + Application.dataPath.Replace('/', '_').Replace(':', '_');
        private static string PREFS_GROUP_ORDER_KEY => "UserQuickAssets_GroupOrder_" + Application.dataPath.Replace('/', '_').Replace(':', '_');

        private static List<string> quickGUIDs = new List<string>();
        private List<string> groupOrder = new List<string>();
        private Dictionary<string, List<Object>> groupedAssets = new Dictionary<string, List<Object>>();

        internal static List<string> GetquickGUIDs()
        {
            if (quickGUIDs == null || (quickGUIDs.Count == 0 && EditorPrefs.HasKey(PREFS_KEY)))
            {
                string data = EditorPrefs.GetString(PREFS_KEY, "");
                quickGUIDs = string.IsNullOrEmpty(data) 
                    ? new List<string>() 
                    : data.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            return quickGUIDs;
        }

        private ScrollView mainScroll;
        private VisualElement dragOverlay;
        private Button toggleModeBtn;
        private Button clearAllBtn;

        private bool isGridView = false; // Tracks current display mode

        [MenuItem("Tools/N2K Toolkit/Quick Assets %q")]
        internal static void ShowWindow()
        {
            if (HasOpenInstances<QuickAssetsWindow>())
            {
                GetWindow<QuickAssetsWindow>().Close();
                return;
            }

            QuickAssetsWindow window = GetWindow<QuickAssetsWindow>("Quick Assets");
            window.minSize = new Vector2(300, 400);
            window.position = new Rect(100, 100, 800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadData();
            // Load the saved display mode (List by default)
            isGridView = EditorPrefs.GetBool(PREFS_MODE_KEY, false);
        }

        internal void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            root.style.paddingTop = 10;
            root.style.paddingBottom = 15;
            root.style.paddingLeft = 15;
            root.style.paddingRight = 15;

            // --- 1. TOOLBAR ---
            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.justifyContent = Justify.FlexEnd;
            toolbar.style.marginBottom = 10;

            clearAllBtn = new Button(ClearAll);
            clearAllBtn.text = "✖ Clear All";
            clearAllBtn.style.width = 90;
            clearAllBtn.style.paddingTop = 4;
            clearAllBtn.style.paddingBottom = 4;
            clearAllBtn.style.marginRight = 8;
            clearAllBtn.style.color = Color.white;
            toolbar.Add(clearAllBtn);

            toggleModeBtn = new Button(ToggleViewMode);
            toggleModeBtn.style.width = 100;
            toggleModeBtn.style.paddingTop = 4;
            toggleModeBtn.style.paddingBottom = 4;
            toolbar.Add(toggleModeBtn);
            root.Add(toolbar);

            // --- 2. MAIN SCROLL VIEW ---
            mainScroll = new ScrollView();
            mainScroll.style.flexGrow = 1;
            mainScroll.contentContainer.style.flexGrow = 1; // Allows empty state to center vertically

            // Group drag-and-drop callbacks for stable coordinate-based reordering
            mainScroll.contentContainer.RegisterCallback<DragUpdatedEvent>(e =>
            {
                string draggedGroup = DragAndDrop.GetGenericData("ReorderQuickGroup") as string;
                if (draggedGroup != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                    int draggedIndex = groupOrder.IndexOf(draggedGroup);
                    if (draggedIndex >= 0)
                    {
                        Vector2 localPos = mainScroll.contentContainer.WorldToLocal(e.mousePosition);

                        int closestChildIndex = -1;
                        float minDistance = float.MaxValue;

                        for (int i = 0; i < mainScroll.contentContainer.childCount; i++)
                        {
                            VisualElement child = mainScroll.contentContainer[i];
                            float dist = Vector2.Distance(localPos, child.layout.center);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                closestChildIndex = i;
                            }
                        }

                        var activeGroups = groupOrder.Where(g => groupedAssets.ContainsKey(g)).ToList();
                        if (closestChildIndex >= 0 && closestChildIndex < activeGroups.Count)
                        {
                            string targetGroup = activeGroups[closestChildIndex];
                            if (targetGroup != draggedGroup)
                            {
                                int targetIndex = groupOrder.IndexOf(targetGroup);
                                if (targetIndex >= 0 && draggedIndex != targetIndex)
                                {
                                    groupOrder.RemoveAt(draggedIndex);
                                    groupOrder.Insert(targetIndex, draggedGroup);

                                    SaveData();
                                    RefreshUI();
                                }
                            }
                        }
                    }
                    e.StopPropagation();
                }
            });

            mainScroll.contentContainer.RegisterCallback<DragPerformEvent>(e =>
            {
                string draggedGroup = DragAndDrop.GetGenericData("ReorderQuickGroup") as string;
                if (draggedGroup != null)
                {
                    DragAndDrop.AcceptDrag();
                    e.StopPropagation();
                }
            });

            root.Add(mainScroll);

            // --- 3. DRAG & DROP OVERLAY ---
            dragOverlay = new VisualElement();
            dragOverlay.style.position = Position.Absolute;
            dragOverlay.style.top = 0;
            dragOverlay.style.bottom = 0;
            dragOverlay.style.left = 0;
            dragOverlay.style.right = 0;
            dragOverlay.style.backgroundColor = new Color(0, 0, 0, 0.75f);
            dragOverlay.style.justifyContent = Justify.Center;
            dragOverlay.style.alignItems = Align.Center;
            dragOverlay.style.display = DisplayStyle.None;

            Image dropIcon = new Image();
            dropIcon.image = EditorGUIUtility.IconContent("d_Import").image;
            dropIcon.style.width = 64;
            dropIcon.style.height = 64;
            dropIcon.style.unityBackgroundImageTintColor = new Color(0.8f, 0.8f, 0.8f);
            dropIcon.style.rotate = new StyleRotate(new Rotate(new Angle(-90, AngleUnit.Degree)));
            dragOverlay.Add(dropIcon);

            Label dropText = new Label("Drop Assets Here");
            dropText.style.fontSize = 24;
            dropText.style.unityFontStyleAndWeight = FontStyle.Bold;
            dropText.style.color = new Color(0.9f, 0.9f, 0.9f);
            dropText.style.marginTop = 10;
            dragOverlay.Add(dropText);

            root.Add(dragOverlay);

            SetupDragAndDrop(root);
            RefreshUI();
        }

        private void ToggleViewMode()
        {
            isGridView = !isGridView;
            EditorPrefs.SetBool(PREFS_MODE_KEY, isGridView);
            RefreshUI();
        }

        internal void RefreshUI()
        {
            mainScroll.Clear();

            // Update the Clear All button's enabled state
            if (clearAllBtn != null)
            {
                clearAllBtn.SetEnabled(quickGUIDs.Count > 0);
            }

            // Update the button text to reflect the current mode
            toggleModeBtn.text = isGridView ? "⊞ Grid View" : "≣ List View";

            // --- EMPTY STATE ---
            if (quickGUIDs.Count == 0)
            {
                VisualElement emptyContainer = new VisualElement();
                emptyContainer.style.flexGrow = 1;
                emptyContainer.style.justifyContent = Justify.Center;
                emptyContainer.style.alignItems = Align.Center;

                Image dropIcon = new Image();
                dropIcon.image = EditorGUIUtility.IconContent("d_Import").image;
                dropIcon.style.width = 64;
                dropIcon.style.height = 64;
                dropIcon.style.unityBackgroundImageTintColor = new Color(0.5f, 0.5f, 0.5f);
                dropIcon.style.rotate = new StyleRotate(new Rotate(new Angle(-90, AngleUnit.Degree)));
                emptyContainer.Add(dropIcon);

                Label dropText = new Label("Drop Assets Here");
                dropText.style.fontSize = 24;
                dropText.style.unityFontStyleAndWeight = FontStyle.Bold;
                dropText.style.color = new Color(0.5f, 0.5f, 0.5f);
                dropText.style.marginTop = 10;
                emptyContainer.Add(dropText);

                mainScroll.Add(emptyContainer);
                return;
            }

            // --- DRAW GROUPS ---
            var orderedGroups = groupedAssets.OrderBy(g => {
                int index = groupOrder.IndexOf(g.Key);
                return index >= 0 ? index : int.MaxValue;
            });

            foreach (var group in orderedGroups)
            {
                VisualElement groupBox = new VisualElement();
                groupBox.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.85f, 0.85f, 0.85f);
                groupBox.style.borderTopLeftRadius = 8;
                groupBox.style.borderTopRightRadius = 8;
                groupBox.style.borderBottomLeftRadius = 8;
                groupBox.style.borderBottomRightRadius = 8;
                groupBox.style.paddingTop = 8;
                groupBox.style.paddingBottom = 8;
                groupBox.style.paddingLeft = 8;
                groupBox.style.paddingRight = 8;
                groupBox.style.marginBottom = 12;

                // Group drag initiation callback
                groupBox.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button == 0)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("ReorderQuickGroup", group.Key);
                        DragAndDrop.StartDrag("Reorder Group");
                        e.StopPropagation();
                    }
                });

                // Header container for group title and remove button
                VisualElement headerContainer = new VisualElement();
                headerContainer.style.flexDirection = FlexDirection.Row;
                headerContainer.style.justifyContent = Justify.SpaceBetween;
                headerContainer.style.alignItems = Align.Center;
                headerContainer.style.marginBottom = 8;

                Label title = new Label(group.Key);
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.fontSize = 14;
                title.style.color = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f);
                headerContainer.Add(title);

                Button removeGroupBtn = new Button(() => RemoveGroup(group.Key));
                removeGroupBtn.text = "✖";
                removeGroupBtn.style.backgroundColor = new Color(0, 0, 0, 0);
                removeGroupBtn.style.borderTopWidth = 0;
                removeGroupBtn.style.borderBottomWidth = 0;
                removeGroupBtn.style.borderLeftWidth = 0;
                removeGroupBtn.style.borderRightWidth = 0;
                removeGroupBtn.style.color = new Color(0.8f, 0.3f, 0.3f);
                removeGroupBtn.style.width = 25;
                removeGroupBtn.style.height = 20;
                removeGroupBtn.style.fontSize = 12;
                removeGroupBtn.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                headerContainer.Add(removeGroupBtn);

                groupBox.Add(headerContainer);

                // Create a container to hold the assets inside this group
                VisualElement groupContent = new VisualElement();

                if (isGridView)
                {
                    // This forces elements to sit side-by-side and wrap when out of space!
                    groupContent.style.flexDirection = FlexDirection.Row;
                    groupContent.style.flexWrap = Wrap.Wrap;
                }
                else
                {
                    // Standard vertical stack for List mode
                    groupContent.style.flexDirection = FlexDirection.Column;
                }

                // Register drag callbacks on groupContent for coordinate-based asset reordering
                groupContent.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    Object draggedObj = DragAndDrop.GetGenericData("ReorderQuickAsset") as Object;
                    if (draggedObj != null)
                    {
                        // Check if the dragged object belongs to this group type
                        string draggedType = draggedObj.GetType().Name;
                        if (draggedType == "GameObject") draggedType = "Prefabs";
                        else if (draggedType == "MonoScript") draggedType = "Scripts";
                        else if (draggedType == "Texture2D") draggedType = "Textures";
                        else draggedType = draggedType + "s";

                        if (draggedType == group.Key)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                            string draggedPath = AssetDatabase.GetAssetPath(draggedObj);
                            if (!string.IsNullOrEmpty(draggedPath))
                            {
                                string draggedGuid = AssetDatabase.AssetPathToGUID(draggedPath);
                                int draggedIndex = quickGUIDs.IndexOf(draggedGuid);

                                if (draggedIndex >= 0)
                                {
                                    Vector2 localPos = groupContent.WorldToLocal(e.mousePosition);

                                    // Find closest item
                                    int closestChildIndex = -1;
                                    float minDistance = float.MaxValue;

                                    for (int i = 0; i < groupContent.childCount; i++)
                                    {
                                        VisualElement child = groupContent[i];
                                        float dist = Vector2.Distance(localPos, child.layout.center);
                                        if (dist < minDistance)
                                        {
                                            minDistance = dist;
                                            closestChildIndex = i;
                                        }
                                    }

                                    if (closestChildIndex >= 0 && closestChildIndex < group.Value.Count)
                                    {
                                        Object targetObj = group.Value[closestChildIndex];
                                        if (targetObj != draggedObj)
                                        {
                                            string targetPath = AssetDatabase.GetAssetPath(targetObj);
                                            string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);
                                            int targetIndex = quickGUIDs.IndexOf(targetGuid);

                                            if (targetIndex >= 0 && draggedIndex != targetIndex)
                                            {
                                                quickGUIDs.RemoveAt(draggedIndex);
                                                quickGUIDs.Insert(targetIndex, draggedGuid);

                                                SaveData();
                                                RefreshUI();
                                            }
                                        }
                                    }
                                }
                            }
                            e.StopPropagation();
                        }
                    }
                });

                groupContent.RegisterCallback<DragPerformEvent>(e =>
                {
                    Object draggedObj = DragAndDrop.GetGenericData("ReorderQuickAsset") as Object;
                    if (draggedObj != null)
                    {
                        DragAndDrop.AcceptDrag();
                        e.StopPropagation();
                    }
                });

                foreach (Object obj in group.Value)
                {
                    if (obj == null) continue;
                    VisualElement itemUI = CreateAssetElement(obj);
                    groupContent.Add(itemUI);
                }

                groupBox.Add(groupContent);
                mainScroll.Add(groupBox);
            }
        }

        private VisualElement CreateAssetElement(Object obj)
        {
            VisualElement container = new VisualElement();
            Color normalColor = new Color(0, 0, 0, 0);
            Color hoverColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.1f) : new Color(0, 0, 0, 0.1f);

            // Hover effects
            container.RegisterCallback<PointerEnterEvent>(e => container.style.backgroundColor = hoverColor);
            container.RegisterCallback<PointerLeaveEvent>(e => container.style.backgroundColor = normalColor);

            // Click to ping object and start drag
            container.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { obj };
                    DragAndDrop.SetGenericData("ReorderQuickAsset", obj);
                    DragAndDrop.StartDrag("Reorder Asset");

                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);

                    e.StopPropagation();
                }
            });

            // 1. GRID MODE (Square Cards)
            if (isGridView)
            {
                container.style.width = 90;
                container.style.height = 90;
                container.style.marginRight = 6;
                container.style.marginBottom = 6;
                container.style.borderTopLeftRadius = 6;
                container.style.borderTopRightRadius = 6;
                container.style.borderBottomLeftRadius = 6;
                container.style.borderBottomRightRadius = 6;
                container.style.alignItems = Align.Center;
                container.style.justifyContent = Justify.Center;

                Image icon = new Image();
                icon.image = AssetPreview.GetMiniThumbnail(obj);
                icon.style.width = 40;
                icon.style.height = 40;
                container.Add(icon);

                Label name = new Label(obj.name);
                name.style.marginTop = 5;
                name.style.fontSize = 10;
                name.style.width = Length.Percent(90);
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                name.style.whiteSpace = WhiteSpace.NoWrap;
                name.style.overflow = Overflow.Hidden;
                container.Add(name);

                Button removeBtn = new Button(() => { RemoveAsset(obj); });
                removeBtn.text = "✖";
                removeBtn.style.position = Position.Absolute;
                removeBtn.style.top = 2;
                removeBtn.style.right = 2;
                removeBtn.style.width = 18;
                removeBtn.style.height = 18;
                removeBtn.style.fontSize = 10;
                removeBtn.style.backgroundColor = new Color(0, 0, 0, 0);
                removeBtn.style.color = new Color(0.8f, 0.3f, 0.3f);
                removeBtn.style.borderTopWidth = 0;
                removeBtn.style.borderBottomWidth = 0;
                removeBtn.style.borderLeftWidth = 0;
                removeBtn.style.borderRightWidth = 0;
                removeBtn.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                container.Add(removeBtn);
            }
            // 2. LIST MODE (Standard Rows)
            else
            {
                container.style.flexDirection = FlexDirection.Row;
                container.style.alignItems = Align.Center;
                container.style.paddingTop = 4;
                container.style.paddingBottom = 4;
                container.style.paddingLeft = 5;
                container.style.paddingRight = 5;
                container.style.borderTopLeftRadius = 4;
                container.style.borderTopRightRadius = 4;
                container.style.borderBottomLeftRadius = 4;
                container.style.borderBottomRightRadius = 4;

                Image icon = new Image();
                icon.image = AssetPreview.GetMiniThumbnail(obj);
                icon.style.width = 18;
                icon.style.height = 18;
                icon.style.marginRight = 8;
                container.Add(icon);

                Label name = new Label(obj.name);
                name.style.flexGrow = 1;
                container.Add(name);

                Button removeBtn = new Button(() => { RemoveAsset(obj); });
                removeBtn.text = "✖";
                removeBtn.style.backgroundColor = new Color(0, 0, 0, 0);
                removeBtn.style.borderTopWidth = 0;
                removeBtn.style.borderBottomWidth = 0;
                removeBtn.style.borderLeftWidth = 0;
                removeBtn.style.borderRightWidth = 0;
                removeBtn.style.color = new Color(0.8f, 0.3f, 0.3f);
                removeBtn.style.width = 25;
                removeBtn.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                container.Add(removeBtn);
            }

            return container;
        }

        private void SetupDragAndDrop(VisualElement root)
        {
            root.RegisterCallback<DragUpdatedEvent>(e =>
            {
                bool isInternalDrag = DragAndDrop.GetGenericData("ReorderQuickAsset") != null || 
                                      DragAndDrop.GetGenericData("ReorderQuickGroup") != null;

                if (isInternalDrag)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    e.StopPropagation();
                }
                else if (DragAndDrop.objectReferences.Length > 0)
                {
                    dragOverlay.style.display = DisplayStyle.Flex;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    e.StopPropagation();
                }
            });

            root.RegisterCallback<DragLeaveEvent>(e => { dragOverlay.style.display = DisplayStyle.None; });
            root.RegisterCallback<DragExitedEvent>(e => { dragOverlay.style.display = DisplayStyle.None; });

            root.RegisterCallback<DragPerformEvent>(e =>
            {
                dragOverlay.style.display = DisplayStyle.None;
                
                bool isInternalDrag = DragAndDrop.GetGenericData("ReorderQuickAsset") != null ||
                                      DragAndDrop.GetGenericData("ReorderQuickGroup") != null;
                if (!isInternalDrag)
                {
                    DragAndDrop.AcceptDrag();
                    bool changed = false;

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        if (!string.IsNullOrEmpty(path))
                        {
                            string guid = AssetDatabase.AssetPathToGUID(path);
                            if (!quickGUIDs.Contains(guid))
                            {
                                quickGUIDs.Add(guid);
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        SaveData();
                        RefreshUI();
                    }
                    e.StopPropagation();
                }
            });
        }

        private void RemoveAsset(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (quickGUIDs.Contains(guid))
            {
                quickGUIDs.Remove(guid);
                SaveData();
                RefreshUI();
            }
        }

        private void LoadData()
        {
            quickGUIDs.Clear();
            string data = EditorPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(data))
            {
                quickGUIDs = data.Split(',').ToList();
            }

            groupOrder.Clear();
            string groupData = EditorPrefs.GetString(PREFS_GROUP_ORDER_KEY, "");
            if (!string.IsNullOrEmpty(groupData))
            {
                groupOrder = groupData.Split(',').ToList();
            }

            RefreshCache();
        }

        private void SaveData()
        {
            EditorPrefs.SetString(PREFS_KEY, string.Join(",", quickGUIDs));
            EditorPrefs.SetString(PREFS_GROUP_ORDER_KEY, string.Join(",", groupOrder));
            RefreshCache();
        }

        public void RefreshCache()
        {
            groupedAssets.Clear();
            List<string> guidsToRemove = new List<string>();

            foreach (string guid in quickGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

                if (obj != null)
                {
                    string typeName = obj.GetType().Name;
                    if (typeName == "GameObject") typeName = "Prefabs";
                    else if (typeName == "MonoScript") typeName = "Scripts";
                    else if (typeName == "Texture2D") typeName = "Textures";
                    else typeName = typeName + "s";

                    if (!groupedAssets.ContainsKey(typeName))
                        groupedAssets[typeName] = new List<Object>();

                    groupedAssets[typeName].Add(obj);
                }
                else
                {
                    guidsToRemove.Add(guid);
                }
            }

            // Sync groupOrder list with currently existing group types
            bool orderChanged = false;
            foreach (string typeName in groupedAssets.Keys)
            {
                if (!groupOrder.Contains(typeName))
                {
                    groupOrder.Add(typeName);
                    orderChanged = true;
                }
            }
            
            // Remove groups that no longer exist
            int removedCount = groupOrder.RemoveAll(gName => !groupedAssets.ContainsKey(gName));
            if (removedCount > 0)
            {
                orderChanged = true;
            }

            if (guidsToRemove.Count > 0)
            {
                foreach (string guid in guidsToRemove) quickGUIDs.Remove(guid);
                orderChanged = true;
            }

            if (orderChanged)
            {
                EditorPrefs.SetString(PREFS_KEY, string.Join(",", quickGUIDs));
                EditorPrefs.SetString(PREFS_GROUP_ORDER_KEY, string.Join(",", groupOrder));
            }
        }

        private void RemoveGroup(string groupName)
        {
            if (EditorUtility.DisplayDialog($"Clear Group: {groupName}", $"Are you sure you want to remove all assets in the {groupName} group?", "Yes", "No"))
            {
                if (groupedAssets.TryGetValue(groupName, out List<Object> assets))
                {
                    List<string> guidsToRemove = new List<string>();
                    foreach (Object obj in assets)
                    {
                        if (obj == null) continue;
                        string path = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(path))
                        {
                            guidsToRemove.Add(AssetDatabase.AssetPathToGUID(path));
                        }
                    }
                    
                    quickGUIDs.RemoveAll(guid => guidsToRemove.Contains(guid));
                    SaveData();
                    RefreshUI();
                }
            }
        }

        private void ClearAll()
        {
            if (EditorUtility.DisplayDialog("Clear All Quick Assets", "Are you sure you want to remove all assets?", "Yes", "No"))
            {
                quickGUIDs.Clear();
                SaveData();
                RefreshUI();
            }
        }
    }
}

#endif