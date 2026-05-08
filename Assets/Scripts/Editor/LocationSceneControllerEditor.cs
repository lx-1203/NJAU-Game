using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(LocationSceneController))]
public class LocationSceneControllerEditor : Editor
{
    private readonly BoxBoundsHandle obstacleHandle = new BoxBoundsHandle();
    private readonly BoxBoundsHandle leftBoundaryHandle = new BoxBoundsHandle();
    private readonly BoxBoundsHandle rightBoundaryHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("预览地点由 previewLocation 决定。钟山台里的“地点可视化编辑”会自动切到对应地点；在 Scene 视图里可直接拖地面、边界、障碍盒，NPC 锚点则由 NPCManager 预览生成。", MessageType.None);
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        LocationSceneController controller = (LocationSceneController)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Rebuild Preview"))
            {
                controller.RebuildScene();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Ensure Dormitory Profile"))
            {
                controller.EnsureDefaultProfiles();
                controller.RebuildScene();
                EditorUtility.SetDirty(controller);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Walls -> Bounds"))
            {
                Undo.RecordObject(controller, "Sync Bounds From Walls");
                controller.SyncActiveBoundsFromWalls();
                controller.RebuildScene();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Bounds -> Walls"))
            {
                Undo.RecordObject(controller, "Snap Walls To Bounds");
                controller.SnapActiveProfileWallsToBounds();
                controller.RebuildScene();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Spawn -> Floor"))
            {
                Undo.RecordObject(controller, "Snap Spawn To Floor");
                controller.SnapActiveSpawnToGround();
                controller.RebuildScene();
                EditorUtility.SetDirty(controller);
            }
        }

        SerializedProperty previewLocationProperty = serializedObject.FindProperty("previewLocation");
        if (GUILayout.Button("Add Profile For Preview Location"))
        {
            Undo.RecordObject(controller, "Add Location Profile");
            controller.AddProfile((LocationId)previewLocationProperty.enumValueIndex);
            controller.RebuildScene();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Add Missing Location Profiles"))
        {
            Undo.RecordObject(controller, "Add Missing Location Profiles");
            controller.AddMissingProfiles();
            controller.RebuildScene();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Select Generated Preview Root"))
        {
            Transform previewRoot = controller.transform.Find("_GeneratedLocationScene");
            Selection.activeObject = previewRoot != null ? previewRoot.gameObject : controller.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();

        SerializedProperty profileProperty = FindActiveProfileProperty();
        if (profileProperty == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawBoundsHandles(profileProperty);
        DrawFloorHandle(profileProperty);
        DrawGroundThicknessHandle(profileProperty);
        DrawSpawnHandle(profileProperty);
        DrawBoundaryHandles(profileProperty);
        DrawObstacleHandles(profileProperty);

        if (serializedObject.ApplyModifiedProperties())
        {
            LocationSceneController controller = (LocationSceneController)target;
            controller.RebuildScene();
            EditorUtility.SetDirty(controller);
        }
    }

    private SerializedProperty FindActiveProfileProperty()
    {
        SerializedProperty profilesProperty = serializedObject.FindProperty("profiles");
        SerializedProperty previewLocationProperty = serializedObject.FindProperty("previewLocation");
        int previewLocation = previewLocationProperty.enumValueIndex;

        for (int i = 0; i < profilesProperty.arraySize; i++)
        {
            SerializedProperty profileProperty = profilesProperty.GetArrayElementAtIndex(i);
            if (profileProperty.FindPropertyRelative("locationId").enumValueIndex == previewLocation)
            {
                return profileProperty;
            }
        }

        return null;
    }

    private void DrawBoundsHandles(SerializedProperty profileProperty)
    {
        LocationSceneController controller = (LocationSceneController)target;
        SerializedProperty minXProperty = profileProperty.FindPropertyRelative("worldMinX");
        SerializedProperty maxXProperty = profileProperty.FindPropertyRelative("worldMaxX");
        SerializedProperty floorYProperty = profileProperty.FindPropertyRelative("floorY");

        float floorY = floorYProperty.floatValue;
        Vector3 minHandlePosition = new Vector3(minXProperty.floatValue, floorY, 0f);
        Vector3 maxHandlePosition = new Vector3(maxXProperty.floatValue, floorY, 0f);

        Handles.color = new Color(0.2f, 0.9f, 1f, 1f);

        EditorGUI.BeginChangeCheck();
        Vector3 newMinPosition = Handles.Slider(minHandlePosition, Vector3.right, HandleUtility.GetHandleSize(minHandlePosition) * 0.12f, Handles.CubeHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Adjust Location Min X");
            minXProperty.floatValue = Mathf.Min(newMinPosition.x, maxXProperty.floatValue - 0.1f);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newMaxPosition = Handles.Slider(maxHandlePosition, Vector3.right, HandleUtility.GetHandleSize(maxHandlePosition) * 0.12f, Handles.CubeHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Adjust Location Max X");
            maxXProperty.floatValue = Mathf.Max(newMaxPosition.x, minXProperty.floatValue + 0.1f);
        }

        Handles.Label(minHandlePosition + Vector3.up * 0.45f, "Min X");
        Handles.Label(maxHandlePosition + Vector3.up * 0.45f, "Max X");
    }

    private void DrawFloorHandle(SerializedProperty profileProperty)
    {
        LocationSceneController controller = (LocationSceneController)target;
        SerializedProperty minXProperty = profileProperty.FindPropertyRelative("worldMinX");
        SerializedProperty maxXProperty = profileProperty.FindPropertyRelative("worldMaxX");
        SerializedProperty floorYProperty = profileProperty.FindPropertyRelative("floorY");

        float centerX = (minXProperty.floatValue + maxXProperty.floatValue) * 0.5f;
        Vector3 floorPosition = new Vector3(centerX, floorYProperty.floatValue, 0f);

        Handles.color = Color.red;
        EditorGUI.BeginChangeCheck();
        Vector3 newFloorPosition = Handles.Slider(floorPosition, Vector3.up, HandleUtility.GetHandleSize(floorPosition) * 0.12f, Handles.SphereHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Adjust Floor Y");
            floorYProperty.floatValue = newFloorPosition.y;
        }

        Handles.Label(floorPosition + Vector3.right * 0.5f, "Floor Y");
    }

    private void DrawSpawnHandle(SerializedProperty profileProperty)
    {
        LocationSceneController controller = (LocationSceneController)target;
        SerializedProperty minXProperty = profileProperty.FindPropertyRelative("worldMinX");
        SerializedProperty maxXProperty = profileProperty.FindPropertyRelative("worldMaxX");
        SerializedProperty spawnYProperty = profileProperty.FindPropertyRelative("spawnY");

        float centerX = (minXProperty.floatValue + maxXProperty.floatValue) * 0.5f;
        Vector3 spawnPosition = new Vector3(centerX, spawnYProperty.floatValue, 0f);

        Handles.color = Color.green;
        EditorGUI.BeginChangeCheck();
        Vector3 newSpawnPosition = Handles.PositionHandle(spawnPosition, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Adjust Spawn Y");
            spawnYProperty.floatValue = newSpawnPosition.y;
        }

        Handles.Label(spawnPosition + Vector3.right * 0.5f, "Spawn Y");
    }

    private void DrawGroundThicknessHandle(SerializedProperty profileProperty)
    {
        LocationSceneController controller = (LocationSceneController)target;
        SerializedProperty minXProperty = profileProperty.FindPropertyRelative("worldMinX");
        SerializedProperty maxXProperty = profileProperty.FindPropertyRelative("worldMaxX");
        SerializedProperty floorYProperty = profileProperty.FindPropertyRelative("floorY");
        SerializedProperty thicknessProperty = profileProperty.FindPropertyRelative("groundThickness");

        float centerX = (minXProperty.floatValue + maxXProperty.floatValue) * 0.5f;
        Vector3 bottomHandlePosition = new Vector3(centerX, floorYProperty.floatValue - thicknessProperty.floatValue, 0f);

        Handles.color = new Color(1f, 0.35f, 0.35f, 1f);
        EditorGUI.BeginChangeCheck();
        Vector3 newBottomPosition = Handles.Slider(bottomHandlePosition, Vector3.up, HandleUtility.GetHandleSize(bottomHandlePosition) * 0.12f, Handles.SphereHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Adjust Ground Thickness");
            thicknessProperty.floatValue = Mathf.Max(0.05f, floorYProperty.floatValue - newBottomPosition.y);
        }

        Handles.Label(bottomHandlePosition + Vector3.right * 0.5f, $"Ground Bottom ({thicknessProperty.floatValue:F2})");
    }

    private void DrawObstacleHandles(SerializedProperty profileProperty)
    {
        LocationSceneController controller = (LocationSceneController)target;
        SerializedProperty obstaclesProperty = profileProperty.FindPropertyRelative("obstacles");
        Handles.color = new Color(1f, 0.45f, 0.2f, 1f);

        for (int i = 0; i < obstaclesProperty.arraySize; i++)
        {
            SerializedProperty obstacleProperty = obstaclesProperty.GetArrayElementAtIndex(i);
            SerializedProperty enabledProperty = obstacleProperty.FindPropertyRelative("enabled");
            if (!enabledProperty.boolValue)
            {
                continue;
            }

            SerializedProperty centerProperty = obstacleProperty.FindPropertyRelative("center");
            SerializedProperty sizeProperty = obstacleProperty.FindPropertyRelative("size");
            SerializedProperty nameProperty = obstacleProperty.FindPropertyRelative("name");

            using (new Handles.DrawingScope(Matrix4x4.identity))
            {
                obstacleHandle.center = new Vector3(centerProperty.vector2Value.x, centerProperty.vector2Value.y, 0f);
                obstacleHandle.size = new Vector3(sizeProperty.vector2Value.x, sizeProperty.vector2Value.y, 0.01f);

                EditorGUI.BeginChangeCheck();
                obstacleHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Adjust Obstacle");
                    centerProperty.vector2Value = new Vector2(obstacleHandle.center.x, obstacleHandle.center.y);
                    sizeProperty.vector2Value = new Vector2(Mathf.Max(0.05f, obstacleHandle.size.x), Mathf.Max(0.05f, obstacleHandle.size.y));
                }
            }

            Vector3 labelPosition = new Vector3(centerProperty.vector2Value.x, centerProperty.vector2Value.y + sizeProperty.vector2Value.y * 0.5f + 0.3f, 0f);
            string label = string.IsNullOrWhiteSpace(nameProperty.stringValue) ? $"Obstacle {i}" : nameProperty.stringValue;
            Handles.Label(labelPosition, label);
        }
    }

    private void DrawBoundaryHandles(SerializedProperty profileProperty)
    {
        SerializedProperty createBoundariesProperty = profileProperty.FindPropertyRelative("createSideBoundaries");
        if (!createBoundariesProperty.boolValue)
        {
            return;
        }

        DrawBoundaryHandle(
            profileProperty.FindPropertyRelative("leftBoundaryCenter"),
            profileProperty.FindPropertyRelative("leftBoundarySize"),
            leftBoundaryHandle,
            new Color(1f, 0.85f, 0.2f, 1f),
            "Left Wall");

        DrawBoundaryHandle(
            profileProperty.FindPropertyRelative("rightBoundaryCenter"),
            profileProperty.FindPropertyRelative("rightBoundarySize"),
            rightBoundaryHandle,
            new Color(1f, 0.75f, 0.1f, 1f),
            "Right Wall");
    }

    private static void DrawBoundaryHandle(
        SerializedProperty centerProperty,
        SerializedProperty sizeProperty,
        BoxBoundsHandle handle,
        Color color,
        string label)
    {
        Handles.color = color;

        using (new Handles.DrawingScope(Matrix4x4.identity))
        {
            handle.center = new Vector3(centerProperty.vector2Value.x, centerProperty.vector2Value.y, 0f);
            handle.size = new Vector3(sizeProperty.vector2Value.x, sizeProperty.vector2Value.y, 0.01f);

            EditorGUI.BeginChangeCheck();
            handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                LocationSceneController controller = (LocationSceneController)centerProperty.serializedObject.targetObject;
                Undo.RecordObject(controller, $"Adjust {label}");
                centerProperty.vector2Value = new Vector2(handle.center.x, handle.center.y);
                sizeProperty.vector2Value = new Vector2(
                    Mathf.Max(0.05f, handle.size.x),
                    Mathf.Max(0.1f, handle.size.y));
            }
        }

        Vector3 labelPosition = new Vector3(
            centerProperty.vector2Value.x,
            centerProperty.vector2Value.y + sizeProperty.vector2Value.y * 0.5f + 0.35f,
            0f);
        Handles.Label(labelPosition, label);
    }
}
