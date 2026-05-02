using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(DormitoryBackgroundController))]
public class DormitoryBackgroundControllerEditor : Editor
{
    private readonly BoxBoundsHandle leftHandle = new BoxBoundsHandle();
    private readonly BoxBoundsHandle rightHandle = new BoxBoundsHandle();
    private readonly BoxBoundsHandle doorHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select the controller in Scene view to drag the generated collider boxes directly. Use Refresh after changing sprite or collision settings.",
            MessageType.Info);

        if (GUILayout.Button("Refresh Background And Colliders"))
        {
            DormitoryBackgroundController controller = (DormitoryBackgroundController)target;
            controller.BuildOrRefreshBackground();
            EditorUtility.SetDirty(controller);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        DormitoryBackgroundController controller = (DormitoryBackgroundController)target;
        if (!controller.TryGetBackgroundBounds(out Bounds bounds))
        {
            return;
        }

        serializedObject.Update();

        SerializedProperty boundaryThicknessProperty = serializedObject.FindProperty("boundaryThickness");
        SerializedProperty boundaryHeightProperty = serializedObject.FindProperty("boundaryHeight");
        SerializedProperty leftBoundaryOffsetProperty = serializedObject.FindProperty("leftBoundaryOffset");
        SerializedProperty rightBoundaryOffsetProperty = serializedObject.FindProperty("rightBoundaryOffset");
        SerializedProperty createDoorBlockerProperty = serializedObject.FindProperty("createDoorBlocker");
        SerializedProperty doorCenterNormalizedXProperty = serializedObject.FindProperty("doorCenterNormalizedX");
        SerializedProperty doorWidthNormalizedProperty = serializedObject.FindProperty("doorWidthNormalized");
        SerializedProperty doorHeightProperty = serializedObject.FindProperty("doorHeight");
        SerializedProperty doorBottomOffsetProperty = serializedObject.FindProperty("doorBottomOffset");
        SerializedProperty doorOffsetProperty = serializedObject.FindProperty("doorOffset");

        float boundaryThickness = Mathf.Max(0.05f, boundaryThicknessProperty.floatValue);
        float boundaryHeight = Mathf.Max(0.1f, boundaryHeightProperty.floatValue);
        float colliderHeight = Mathf.Max(boundaryHeight, bounds.size.y + 2f);
        float centerY = bounds.center.y;

        Vector2 leftOffset = leftBoundaryOffsetProperty.vector2Value;
        Vector2 rightOffset = rightBoundaryOffsetProperty.vector2Value;

        DrawBoundaryHandle(
            leftHandle,
            "Left Boundary",
            bounds.min.x - boundaryThickness * 0.5f + leftOffset.x,
            centerY + leftOffset.y,
            boundaryThickness,
            colliderHeight,
            leftBoundaryOffsetProperty,
            bounds.min.x,
            centerY,
            boundaryThicknessProperty,
            boundaryHeightProperty);

        DrawBoundaryHandle(
            rightHandle,
            "Right Boundary",
            bounds.max.x + boundaryThickness * 0.5f + rightOffset.x,
            centerY + rightOffset.y,
            boundaryThickness,
            colliderHeight,
            rightBoundaryOffsetProperty,
            bounds.max.x,
            centerY,
            boundaryThicknessProperty,
            boundaryHeightProperty);

        if (createDoorBlockerProperty.boolValue)
        {
            float doorWidth = Mathf.Max(0.5f, bounds.size.x * Mathf.Max(0.01f, doorWidthNormalizedProperty.floatValue));
            float doorCenterX = Mathf.Lerp(bounds.min.x, bounds.max.x, doorCenterNormalizedXProperty.floatValue) + doorOffsetProperty.vector2Value.x;
            float doorCenterY = bounds.min.y + doorBottomOffsetProperty.floatValue + doorHeightProperty.floatValue * 0.5f + doorOffsetProperty.vector2Value.y;

            Handles.color = new Color(1f, 0.5f, 0f, 1f);
            doorHandle.center = new Vector3(doorCenterX, doorCenterY, 0f);
            doorHandle.size = new Vector3(doorWidth, Mathf.Max(0.1f, doorHeightProperty.floatValue), 0.01f);

            EditorGUI.BeginChangeCheck();
            doorHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, doorHandle.center.x);
                float normalizedWidth = bounds.size.x > 0.001f ? doorHandle.size.x / bounds.size.x : 0.075f;
                Vector2 newDoorOffset = doorOffsetProperty.vector2Value;
                float clampedNormalizedX = Mathf.Clamp01(normalizedX);
                newDoorOffset.x = doorHandle.center.x - Mathf.Lerp(bounds.min.x, bounds.max.x, clampedNormalizedX);
                doorCenterNormalizedXProperty.floatValue = clampedNormalizedX;
                doorWidthNormalizedProperty.floatValue = Mathf.Clamp(normalizedWidth, 0.01f, 0.3f);
                doorHeightProperty.floatValue = Mathf.Max(0.1f, doorHandle.size.y);

                float baseDoorCenterY = bounds.min.y + doorBottomOffsetProperty.floatValue + doorHeightProperty.floatValue * 0.5f;
                newDoorOffset.y = doorHandle.center.y - baseDoorCenterY;
                doorOffsetProperty.vector2Value = newDoorOffset;
            }

            Handles.Label(new Vector3(doorCenterX, doorCenterY + doorHandle.size.y * 0.5f + 0.3f, 0f), "Door Blocker");
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            controller.BuildOrRefreshBackground();
            EditorUtility.SetDirty(controller);
        }
    }

    private void DrawBoundaryHandle(
        BoxBoundsHandle handle,
        string label,
        float centerX,
        float centerY,
        float width,
        float height,
        SerializedProperty offsetProperty,
        float edgeX,
        float baseCenterY,
        SerializedProperty boundaryThicknessProperty,
        SerializedProperty boundaryHeightProperty)
    {
        Handles.color = Color.yellow;
        handle.center = new Vector3(centerX, centerY, 0f);
        handle.size = new Vector3(width, height, 0.01f);

        EditorGUI.BeginChangeCheck();
        handle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Vector2 offset = offsetProperty.vector2Value;
            float newWidth = Mathf.Max(0.05f, handle.size.x);
            offset.x = handle.center.x - (edgeX + Mathf.Sign(centerX - edgeX) * newWidth * 0.5f);
            offset.y = handle.center.y - baseCenterY;
            offsetProperty.vector2Value = offset;
            boundaryThicknessProperty.floatValue = newWidth;
            boundaryHeightProperty.floatValue = Mathf.Max(0.1f, handle.size.y);
        }

        Handles.Label(new Vector3(centerX, centerY + height * 0.5f + 0.3f, 0f), label);
    }
}
