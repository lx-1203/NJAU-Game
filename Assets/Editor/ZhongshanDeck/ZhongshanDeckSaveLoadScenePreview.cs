using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ZhongshanDeckSaveLoadScenePreview
{
    private const string SaveLoadPreviewScenePath = "Assets/Scenes/SaveLoadPreview.unity";
    private const string PreviewRootName = "SaveLoadUIEditorPreview";

    private static string selectedLayoutKey = ZhongshanDeckSaveLoadContentDefaults.LayoutBoard;
    private static bool previewAsSaveMode;
    private static bool previewVisible = false;
    private static bool isDragging;
    private static bool isResizing;
    private static Vector2 dragStartMouse;
    private static Vector2 dragStartPosition;
    private static Vector2 dragStartSize;
    private static Vector2 dragStartRenderedSize;
    private static Vector2 dragCanvasScale;
    private static bool hasPendingSave;
    private static double lastPreviewSyncTime;

    static ZhongshanDeckSaveLoadScenePreview()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            ResetDragState();
        }
    }

    private static void OnUndoRedoPerformed()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.path, SaveLoadPreviewScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ResetDragState();

        SaveLoadUI preview = FindExistingPreview(activeScene);
        if (preview != null && previewVisible)
        {
            preview.EditorSyncLivePreview();
        }

        SceneView.RepaintAll();
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.path, SaveLoadPreviewScenePath, StringComparison.OrdinalIgnoreCase))
        {
            ResetDragState();
            return;
        }

        ZhongshanDeckToolState asset = ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        asset.EnsureInitialized();
        ZhongshanDeckSaveLoadContent content = asset.saveLoadContent;
        if (content == null)
        {
            return;
        }

        content.EnsureInitialized();
        EnsureSelectedLayoutItem(content);

        Handles.BeginGUI();
        DrawToolbar(sceneView, activeScene);
        Handles.EndGUI();

        if (!previewVisible)
        {
            SetPreviewVisibility(activeScene, false);
            ResetDragState();
            return;
        }

        SaveLoadUI preview = FindOrCreatePreview(activeScene);
        if (preview == null)
        {
            return;
        }

        SetPreviewVisibility(activeScene, true);

        if (!preview.EditorPreviewIsBuilt())
        {
            preview.EditorBuildLivePreview(previewAsSaveMode);
        }
        else if (EditorApplication.timeSinceStartup - lastPreviewSyncTime > 0.35d)
        {
            preview.EditorSyncLivePreview();
            lastPreviewSyncTime = EditorApplication.timeSinceStartup;
        }

        Event evt = Event.current;
        if (evt.type == UnityEngine.EventType.Layout && (isDragging || isResizing))
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        if (IsSceneNavigationEvent(evt))
        {
            ResetDragState();
            return;
        }

        Handles.BeginGUI();
        DrawLayoutHandles(content, asset, preview, evt);
        DrawSelectionInfo(content, preview);
        Handles.EndGUI();
    }

    private static void DrawToolbar(SceneView sceneView, Scene activeScene)
    {
        Rect barRect = new Rect(14f, 44f, Mathf.Max(520f, sceneView.position.width - 28f), 26f);
        GUILayout.BeginArea(barRect);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("存档界面真实预览", EditorStyles.boldLabel, GUILayout.Width(110f));
            GUILayout.Label(previewVisible ? "当前 Scene 中显示的是 SaveLoadUI 真实构建出的存档面板。" : "存档预览已隐藏，需要时可再次显示。", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            bool newVisible = GUILayout.Toggle(previewVisible, previewVisible ? "隐藏预览" : "显示预览", EditorStyles.toolbarButton, GUILayout.Width(86f));
            if (newVisible != previewVisible)
            {
                previewVisible = newVisible;
                SetPreviewVisibility(activeScene, previewVisible);
                if (previewVisible)
                {
                    SaveLoadUI preview = FindOrCreatePreview(activeScene);
                    if (preview != null)
                    {
                        preview.EditorBuildLivePreview(previewAsSaveMode);
                    }
                }

                SceneView.RepaintAll();
            }

            if (GUILayout.Button("单独编辑存档", GUILayout.Width(96f)))
            {
                previewVisible = true;
                SetPreviewVisibility(activeScene, true);
                SaveLoadUI preview = FindOrCreatePreview(activeScene);
                if (preview != null)
                {
                    preview.EditorBuildLivePreview(previewAsSaveMode);
                }

                SceneView.RepaintAll();
            }

            bool newSaveMode = GUILayout.Toggle(previewAsSaveMode, previewAsSaveMode ? "保存模式预览" : "读档模式预览", EditorStyles.toolbarButton, GUILayout.Width(110f));
            if (newSaveMode != previewAsSaveMode)
            {
                previewAsSaveMode = newSaveMode;
                SaveLoadUI preview = FindExistingPreview(activeScene);
                if (preview != null)
                {
                    preview.EditorBuildLivePreview(previewAsSaveMode);
                }
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("重建真实预览", GUILayout.Width(110f)))
            {
                SaveLoadUI preview = FindOrCreatePreview(activeScene);
                if (preview != null)
                {
                    preview.EditorBuildLivePreview(previewAsSaveMode);
                }
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("打开钟山台场景跳转", GUILayout.Width(150f)))
            {
                ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.SceneJump);
            }
        }
        GUILayout.EndArea();
    }

    private static void DrawLayoutHandles(ZhongshanDeckSaveLoadContent content, ZhongshanDeckToolState asset, SaveLoadUI preview, Event evt)
    {
        ZhongshanDeckSaveLoadLayoutItem clickedItem = null;

        for (int i = content.layoutItems.Count - 1; i >= 0; i--)
        {
            ZhongshanDeckSaveLoadLayoutItem item = content.layoutItems[i];
            if (item == null)
            {
                continue;
            }

            if (ShouldSkipInteractiveItem(item.key))
            {
                continue;
            }

            item.EnsureInitialized();
            RectTransform target = preview.EditorGetLayoutRect(item.key);
            Rect guiRect = GetGUIRect(target);
            if (guiRect.width <= 1f || guiRect.height <= 1f)
            {
                continue;
            }

            bool isSelected = string.Equals(selectedLayoutKey, item.key, StringComparison.Ordinal);
            bool isLocked = item.locked;
            Color fill = item.visible
                ? (isSelected ? new Color(0.98f, 0.76f, 0.25f, 0.14f) : new Color(0.3f, 0.7f, 1f, 0.08f))
                : new Color(0.45f, 0.45f, 0.45f, 0.08f);
            Color outline = isLocked
                ? new Color(1f, 0.45f, 0.45f, isSelected ? 0.95f : 0.82f)
                : item.visible
                ? (isSelected ? new Color(1f, 0.84f, 0.3f, 0.95f) : new Color(0.52f, 0.8f, 1f, 0.9f))
                : new Color(0.72f, 0.72f, 0.72f, 0.6f);

            EditorGUI.DrawRect(guiRect, fill);
            Handles.color = outline;
            Handles.DrawSolidRectangleWithOutline(guiRect, Color.clear, outline);
            DrawItemCaption(guiRect, item, outline);

            Rect resizeHandle = new Rect(guiRect.xMax - 12f, guiRect.yMax - 12f, 12f, 12f);
            if (!isLocked)
            {
                EditorGUI.DrawRect(resizeHandle, outline);
            }

            if (evt.type == UnityEngine.EventType.MouseDown && evt.button == 0)
            {
                if (resizeHandle.Contains(evt.mousePosition) && !isLocked)
                {
                    BeginEditingItem(asset, item, target, guiRect, evt.mousePosition, true);
                    clickedItem = item;
                    evt.Use();
                }
                else if (guiRect.Contains(evt.mousePosition))
                {
                    selectedLayoutKey = item.key;
                    if (!isLocked)
                    {
                        BeginEditingItem(asset, item, target, guiRect, evt.mousePosition, false);
                    }
                    clickedItem = item;
                    evt.Use();
                }
            }
        }

        ZhongshanDeckSaveLoadLayoutItem selectedItem = GetSelectedItem(content);
        if (selectedItem != null)
        {
            if (evt.type == UnityEngine.EventType.MouseDrag && (isDragging || isResizing))
            {
                ApplyDragDelta(selectedItem, preview, evt.mousePosition);
                EditorUtility.SetDirty(asset);
                hasPendingSave = true;
                evt.Use();
                SceneView.RepaintAll();
            }

            if (evt.type == UnityEngine.EventType.MouseUp && evt.button == 0)
            {
                if (hasPendingSave)
                {
                    AssetDatabase.SaveAssets();
                }

                ResetDragState();
                evt.Use();
            }
        }

        if (clickedItem == null && evt.type == UnityEngine.EventType.MouseDown && evt.button == 0)
        {
            SceneView.RepaintAll();
        }
    }

    private static void DrawItemCaption(Rect rect, ZhongshanDeckSaveLoadLayoutItem item, Color accent)
    {
        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, Mathf.Min(160f, rect.width - 12f), 18f);
        EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.55f));
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = accent },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };
        string label = item.locked ? $"{item.displayName} [锁]" : item.displayName;
        GUI.Label(labelRect, label, style);
    }

    private static void DrawSelectionInfo(ZhongshanDeckSaveLoadContent content, SaveLoadUI preview)
    {
        ZhongshanDeckSaveLoadLayoutItem selectedItem = GetSelectedItem(content);
        if (selectedItem == null)
        {
            return;
        }

        RectTransform target = preview.EditorGetLayoutRect(selectedItem.key);
        Rect guiRect = GetGUIRect(target);
        Rect infoRect = new Rect(guiRect.xMin, Mathf.Max(82f, guiRect.yMin - 32f), 430f, 24f);
        EditorGUI.DrawRect(infoRect, new Color(0f, 0f, 0f, 0.58f));
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11
        };
        string text = $"  {selectedItem.displayName} | {(selectedItem.locked ? "已锁定" : "可编辑")} | 锚点 {GetAnchorLabel(selectedItem.anchor)} | 位置 {selectedItem.anchoredPosition.x:0}, {selectedItem.anchoredPosition.y:0} | 尺寸 {selectedItem.size.x:0} x {selectedItem.size.y:0}";
        GUI.Label(infoRect, text, style);
    }

    private static void BeginEditingItem(ZhongshanDeckToolState asset, ZhongshanDeckSaveLoadLayoutItem item, RectTransform target, Rect guiRect, Vector2 mousePosition, bool resize)
    {
        Undo.RecordObject(asset, resize ? "Resize Save Layout Item" : "Move Save Layout Item");
        selectedLayoutKey = item.key;
        isDragging = !resize;
        isResizing = resize;
        dragStartMouse = mousePosition;
        dragStartPosition = item.anchoredPosition;
        dragStartSize = item.size;
        dragStartRenderedSize = guiRect.size;
        dragCanvasScale = GetCanvasScale(target, guiRect);
        hasPendingSave = false;
    }

    private static void ApplyDragDelta(ZhongshanDeckSaveLoadLayoutItem item, SaveLoadUI preview, Vector2 mousePosition)
    {
        RectTransform target = preview.EditorGetLayoutRect(item.key);
        if (target == null)
        {
            return;
        }

        Vector2 guiDelta = mousePosition - dragStartMouse;
        if (isDragging)
        {
            item.anchoredPosition = dragStartPosition + new Vector2(
                guiDelta.x / Mathf.Max(0.0001f, dragCanvasScale.x),
                -guiDelta.y / Mathf.Max(0.0001f, dragCanvasScale.y));
        }
        else if (isResizing)
        {
            float desiredWidth = Mathf.Max(24f, dragStartRenderedSize.x + guiDelta.x);
            float desiredHeight = Mathf.Max(24f, dragStartRenderedSize.y + guiDelta.y);
            item.size = new Vector2(
                desiredWidth / Mathf.Max(0.0001f, dragCanvasScale.x),
                desiredHeight / Mathf.Max(0.0001f, dragCanvasScale.y));
        }

        preview.EditorApplyLayoutPreview();
    }

    private static Vector2 GetCanvasScale(RectTransform target, Rect guiRect)
    {
        if (target == null)
        {
            return Vector2.one;
        }

        float renderedBaseWidth = Mathf.Max(1f, target.rect.width * Mathf.Abs(target.localScale.x));
        float renderedBaseHeight = Mathf.Max(1f, target.rect.height * Mathf.Abs(target.localScale.y));
        return new Vector2(
            guiRect.width / renderedBaseWidth,
            guiRect.height / renderedBaseHeight);
    }

    private static Rect GetGUIRect(RectTransform rectTransform)
    {
        if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
        {
            return Rect.zero;
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = HandleUtility.WorldToGUIPoint(corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        if (float.IsInfinity(min.x) || float.IsInfinity(min.y) || float.IsInfinity(max.x) || float.IsInfinity(max.y))
        {
            return Rect.zero;
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void SetPreviewVisibility(Scene activeScene, bool visible)
    {
        SaveLoadUI preview = CleanupDuplicatePreviews(activeScene);
        if (preview != null)
        {
            preview.gameObject.SetActive(visible);
        }
    }

    private static SaveLoadUI FindExistingPreview(Scene activeScene)
    {
        SaveLoadUI[] previews = FindScenePreviews(activeScene);
        SaveLoadUI preferred = null;

        for (int i = 0; i < previews.Length; i++)
        {
            SaveLoadUI preview = previews[i];
            if (preview == null)
            {
                continue;
            }

            if (string.Equals(preview.gameObject.name, PreviewRootName, StringComparison.Ordinal))
            {
                preferred = preview;
                break;
            }

            if (preferred == null)
            {
                preferred = preview;
            }
        }

        return preferred;
    }

    private static SaveLoadUI[] FindScenePreviews(Scene activeScene)
    {
        SaveLoadUI[] previews = Resources.FindObjectsOfTypeAll<SaveLoadUI>();
        System.Collections.Generic.List<SaveLoadUI> matches = new System.Collections.Generic.List<SaveLoadUI>();
        for (int i = 0; i < previews.Length; i++)
        {
            SaveLoadUI preview = previews[i];
            if (preview != null && preview.gameObject.scene == activeScene)
            {
                matches.Add(preview);
            }
        }

        return matches.ToArray();
    }

    private static SaveLoadUI CleanupDuplicatePreviews(Scene activeScene)
    {
        SaveLoadUI[] previews = FindScenePreviews(activeScene);
        SaveLoadUI keeper = null;

        for (int i = 0; i < previews.Length; i++)
        {
            SaveLoadUI preview = previews[i];
            if (preview == null)
            {
                continue;
            }

            bool isPreferredName = string.Equals(preview.gameObject.name, PreviewRootName, StringComparison.Ordinal);
            if (keeper == null || (isPreferredName && !string.Equals(keeper.gameObject.name, PreviewRootName, StringComparison.Ordinal)))
            {
                keeper = preview;
            }
        }

        for (int i = 0; i < previews.Length; i++)
        {
            SaveLoadUI preview = previews[i];
            if (preview == null || preview == keeper)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(preview.gameObject);
        }

        if (keeper != null && !string.Equals(keeper.gameObject.name, PreviewRootName, StringComparison.Ordinal))
        {
            keeper.gameObject.name = PreviewRootName;
            keeper.gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }

        return keeper;
    }

    private static SaveLoadUI FindOrCreatePreview(Scene activeScene)
    {
        SaveLoadUI existing = CleanupDuplicatePreviews(activeScene);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject(PreviewRootName);
        go.hideFlags = HideFlags.DontSaveInEditor;
        SceneManager.MoveGameObjectToScene(go, activeScene);
        return go.AddComponent<SaveLoadUI>();
    }

    private static ZhongshanDeckSaveLoadLayoutItem GetSelectedItem(ZhongshanDeckSaveLoadContent content)
    {
        if (content?.layoutItems == null)
        {
            return null;
        }

        for (int i = 0; i < content.layoutItems.Count; i++)
        {
            ZhongshanDeckSaveLoadLayoutItem item = content.layoutItems[i];
            if (item != null && string.Equals(item.key, selectedLayoutKey, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private static void EnsureSelectedLayoutItem(ZhongshanDeckSaveLoadContent content)
    {
        ZhongshanDeckSaveLoadLayoutItem selectedItem = GetSelectedItem(content);
        if (selectedItem != null && !ShouldSkipInteractiveItem(selectedItem.key))
        {
            return;
        }

        if (content?.layoutItems == null)
        {
            return;
        }

        for (int i = 0; i < content.layoutItems.Count; i++)
        {
            ZhongshanDeckSaveLoadLayoutItem item = content.layoutItems[i];
            if (item == null || ShouldSkipInteractiveItem(item.key))
            {
                continue;
            }

            selectedLayoutKey = item.key;
            return;
        }
    }

    private static string GetAnchorLabel(ZhongshanDeckLayoutAnchor anchor)
    {
        switch (anchor)
        {
            case ZhongshanDeckLayoutAnchor.TopLeft: return "左上";
            case ZhongshanDeckLayoutAnchor.TopCenter: return "上中";
            case ZhongshanDeckLayoutAnchor.TopRight: return "右上";
            case ZhongshanDeckLayoutAnchor.LeftCenter: return "左中";
            case ZhongshanDeckLayoutAnchor.Center: return "居中";
            case ZhongshanDeckLayoutAnchor.RightCenter: return "右中";
            case ZhongshanDeckLayoutAnchor.BottomLeft: return "左下";
            case ZhongshanDeckLayoutAnchor.BottomCenter: return "下中";
            case ZhongshanDeckLayoutAnchor.BottomRight: return "右下";
            default: return anchor.ToString();
        }
    }

    private static void ResetDragState()
    {
        isDragging = false;
        isResizing = false;
        hasPendingSave = false;
    }

    private static bool ShouldSkipInteractiveItem(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        for (int slot = 0; slot < 4; slot++)
        {
            if (string.Equals(key, ZhongshanDeckSaveLoadContentDefaults.GetSlotButtonsKey(slot), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static void SetPreviewVisible(bool visible)
    {
        previewVisible = visible;
        if (!visible)
        {
            ResetDragState();
            HideAllPreviewInstances();
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.path, SaveLoadPreviewScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SetPreviewVisibility(activeScene, visible);
        if (visible)
        {
            SaveLoadUI preview = FindOrCreatePreview(activeScene);
            if (preview != null)
            {
                preview.EditorBuildLivePreview(previewAsSaveMode);
            }
        }

        SceneView.RepaintAll();
    }

    public static void HideAllPreviewInstances()
    {
        SaveLoadUI[] previews = Resources.FindObjectsOfTypeAll<SaveLoadUI>();
        for (int i = 0; i < previews.Length; i++)
        {
            SaveLoadUI preview = previews[i];
            if (preview == null)
            {
                continue;
            }

            GameObject go = preview.gameObject;
            if (go == null)
            {
                continue;
            }

            if (string.Equals(go.name, PreviewRootName, StringComparison.Ordinal))
            {
                go.SetActive(false);
            }
        }
    }

    private static bool IsSceneNavigationEvent(Event evt)
    {
        if (evt == null)
        {
            return false;
        }

        if (evt.alt || evt.button == 1 || evt.button == 2)
        {
            return true;
        }

        return Tools.viewToolActive;
    }
}
