using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ZhongshanDeckTitleScenePreview
{
    private const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";

    private static string selectedLayoutKey = ZhongshanDeckTitleContentDefaults.LayoutLogo;
    private static bool previewVisible = true;
    private static bool isDragging;
    private static bool isResizing;
    private static Vector2 dragStartMouse;
    private static Vector2 dragStartPosition;
    private static Vector2 dragStartSize;
    private static Vector2 dragStartRenderedSize;
    private static Vector2 dragCanvasScale;
    private static bool hasPendingSave;
    private static double lastPreviewSyncTime;

    static ZhongshanDeckTitleScenePreview()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            ResetDragState();
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.path, TitleScenePath, StringComparison.OrdinalIgnoreCase))
        {
            ResetDragState();
            return;
        }

        ZhongshanDeckSaveLoadScenePreview.HideAllPreviewInstances();

        TitleScreenManager manager = FindSceneTitleScreenManager(activeScene);
        if (manager == null)
        {
            return;
        }

        Handles.BeginGUI();
        DrawToolbar(sceneView, manager);
        Handles.EndGUI();

        if (!previewVisible)
        {
            SetPreviewVisibility(manager, false);
            ResetDragState();
            return;
        }

        SetPreviewVisibility(manager, true);

        if (!manager.EditorPreviewIsBuilt())
        {
            manager.EditorBuildLivePreview();
        }
        else if (EditorApplication.timeSinceStartup - lastPreviewSyncTime > 0.35d)
        {
            manager.EditorSyncLivePreview();
            lastPreviewSyncTime = EditorApplication.timeSinceStartup;
        }

        ZhongshanDeckToolState asset = ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        asset.EnsureInitialized();
        ZhongshanDeckHomepageContent homepage = asset.titleContent?.homepage;
        if (homepage == null)
        {
            return;
        }

        ZhongshanDeckTitleContentDefaults.EnsureHomepageLayoutItems(homepage.layoutItems);
        EnsureSelectedLayoutItem(homepage);

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
        DrawLayoutHandles(homepage, asset, manager, evt);
        DrawSelectionInfo(homepage, manager);
        Handles.EndGUI();
    }

    private static void DrawToolbar(SceneView sceneView, TitleScreenManager manager)
    {
        Rect barRect = new Rect(14f, 14f, Mathf.Max(560f, sceneView.position.width - 28f), 26f);
        GUILayout.BeginArea(barRect);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("标题页真实预览", EditorStyles.boldLabel, GUILayout.Width(100f));
            GUILayout.Label(previewVisible ? "当前 Scene 中显示的是 TitleScreenManager 真实构建出的标题页。" : "首页预览已隐藏，需要时可再次显示。", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            bool newVisible = GUILayout.Toggle(previewVisible, previewVisible ? "隐藏预览" : "显示预览", EditorStyles.toolbarButton, GUILayout.Width(86f));
            if (newVisible != previewVisible)
            {
                previewVisible = newVisible;
                SetPreviewVisibility(manager, previewVisible);
                if (previewVisible)
                {
                    manager.EditorBuildLivePreview();
                }

                SceneView.RepaintAll();
            }

            if (GUILayout.Button("单独编辑首页", GUILayout.Width(96f)))
            {
                previewVisible = true;
                SetPreviewVisibility(manager, true);
                if (!manager.EditorPreviewIsBuilt())
                {
                    manager.EditorBuildLivePreview();
                }

                ZhongshanDeckSaveLoadScenePreview.SetPreviewVisible(false);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("重建真实预览", GUILayout.Width(110f)))
            {
                manager.EditorBuildLivePreview();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("打开钟山台内容页", GUILayout.Width(130f)))
            {
                ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Content);
            }
        }
        GUILayout.EndArea();
    }

    private static void DrawLayoutHandles(ZhongshanDeckHomepageContent homepage, ZhongshanDeckToolState asset, TitleScreenManager manager, Event evt)
    {
        ZhongshanDeckHomepageLayoutItem clickedItem = null;

        for (int i = homepage.layoutItems.Count - 1; i >= 0; i--)
        {
            ZhongshanDeckHomepageLayoutItem item = homepage.layoutItems[i];
            if (item == null)
            {
                continue;
            }

            item.EnsureInitialized();
            RectTransform target = manager.EditorGetHomepageLayoutRect(item.key);
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

        ZhongshanDeckHomepageLayoutItem selectedItem = GetSelectedItem(homepage);
        if (selectedItem != null)
        {
            if (evt.type == UnityEngine.EventType.MouseDrag && (isDragging || isResizing))
            {
                ApplyDragDelta(selectedItem, manager, evt.mousePosition);
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

    private static void DrawItemCaption(Rect rect, ZhongshanDeckHomepageLayoutItem item, Color accent)
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

    private static void DrawSelectionInfo(ZhongshanDeckHomepageContent homepage, TitleScreenManager manager)
    {
        ZhongshanDeckHomepageLayoutItem selectedItem = GetSelectedItem(homepage);
        if (selectedItem == null)
        {
            return;
        }

        RectTransform target = manager.EditorGetHomepageLayoutRect(selectedItem.key);
        Rect guiRect = GetGUIRect(target);
        Rect infoRect = new Rect(guiRect.xMin, Mathf.Max(52f, guiRect.yMin - 32f), 430f, 24f);
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

    private static void BeginEditingItem(ZhongshanDeckToolState asset, ZhongshanDeckHomepageLayoutItem item, RectTransform target, Rect guiRect, Vector2 mousePosition, bool resize)
    {
        Undo.RecordObject(asset, resize ? "Resize Title Layout Item" : "Move Title Layout Item");
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

    private static void ApplyDragDelta(ZhongshanDeckHomepageLayoutItem item, TitleScreenManager manager, Vector2 mousePosition)
    {
        RectTransform target = manager.EditorGetHomepageLayoutRect(item.key);
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
                ConvertRenderedSizeToAssetSize(item.key, desiredWidth, dragCanvasScale.x),
                ConvertRenderedSizeToAssetSize(item.key, desiredHeight, dragCanvasScale.y, false));
        }

        manager.EditorApplyHomepageLayoutPreview();
    }

    private static float ConvertRenderedSizeToAssetSize(string key, float desiredRenderedSize, float canvasScale, bool horizontal = true)
    {
        desiredRenderedSize = Mathf.Max(24f, desiredRenderedSize);
        canvasScale = Mathf.Max(0.0001f, canvasScale);

        if (TitleScreenManager.EditorUsesScaledLayout(key))
        {
            Vector2 defaultSize = TitleScreenManager.EditorGetDefaultHomepageLayoutSize(key);
            float defaultAxis = horizontal ? defaultSize.x : defaultSize.y;
            float assetSize = Mathf.Sqrt((desiredRenderedSize * defaultAxis) / canvasScale);
            return Mathf.Max(24f, assetSize);
        }

        return Mathf.Max(24f, desiredRenderedSize / canvasScale);
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

    private static ZhongshanDeckHomepageLayoutItem GetSelectedItem(ZhongshanDeckHomepageContent homepage)
    {
        if (homepage?.layoutItems == null)
        {
            return null;
        }

        for (int i = 0; i < homepage.layoutItems.Count; i++)
        {
            ZhongshanDeckHomepageLayoutItem item = homepage.layoutItems[i];
            if (item != null && string.Equals(item.key, selectedLayoutKey, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private static void EnsureSelectedLayoutItem(ZhongshanDeckHomepageContent homepage)
    {
        if (GetSelectedItem(homepage) != null)
        {
            return;
        }

        if (homepage?.layoutItems != null && homepage.layoutItems.Count > 0 && homepage.layoutItems[0] != null)
        {
            selectedLayoutKey = homepage.layoutItems[0].key;
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

    private static TitleScreenManager FindSceneTitleScreenManager(Scene activeScene)
    {
        TitleScreenManager[] managers = Resources.FindObjectsOfTypeAll<TitleScreenManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            TitleScreenManager manager = managers[i];
            if (manager != null && manager.gameObject.scene == activeScene)
            {
                return manager;
            }
        }

        return null;
    }

    private static void ResetDragState()
    {
        isDragging = false;
        isResizing = false;
        hasPendingSave = false;
    }

    public static void SetPreviewVisible(bool visible)
    {
        previewVisible = visible;
        if (!visible)
        {
            ResetDragState();
        }
        else
        {
            ZhongshanDeckSaveLoadScenePreview.HideAllPreviewInstances();
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.path, TitleScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TitleScreenManager manager = FindSceneTitleScreenManager(activeScene);
        if (manager == null)
        {
            return;
        }

        SetPreviewVisibility(manager, visible);
        if (visible && !manager.EditorPreviewIsBuilt())
        {
            manager.EditorBuildLivePreview();
        }

        SceneView.RepaintAll();
    }

    private static void SetPreviewVisibility(TitleScreenManager manager, bool visible)
    {
        if (manager == null)
        {
            return;
        }

        manager.EditorSetPreviewVisible(visible);
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
