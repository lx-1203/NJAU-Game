using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    private readonly BoxBoundsHandle colliderBoundsHandle = new BoxBoundsHandle();
    private readonly BoxBoundsHandle visualBoundsHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select the player in Scene view to drag the yellow visual box, cyan collision box, and red ground check sphere directly.",
            MessageType.Info);

        PlayerController controller = (PlayerController)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("启用可视高度同步"))
            {
                Undo.RecordObject(controller, "Enable Visual Height Sync");
                controller.AutoScaleToVisualHeight = true;
                SpriteRenderer renderer = controller.GetSpriteRenderer();
                if (renderer != null && renderer.sprite != null)
                {
                    controller.SetVisualHeight(renderer.bounds.size.y);
                }
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Snap Ground To Collider Bottom"))
            {
                Undo.RecordObject(controller, "Snap Ground Check");
                controller.SnapGroundCheckToColliderBottom();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Focus Ground Check"))
            {
                if (controller.GroundCheckTransform != null)
                {
                    Selection.activeTransform = controller.GroundCheckTransform;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        PlayerController controller = (PlayerController)target;
        DrawGroundCheckHandle(controller);
        DrawVisualBoundsHandle(controller);
        DrawColliderHandle(controller);
        DrawVisualHeightHandle(controller);
    }

    private void DrawVisualBoundsHandle(PlayerController controller)
    {
        SpriteRenderer renderer = controller.GetSpriteRenderer();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Bounds bounds = renderer.bounds;
        float originalBottomY = bounds.min.y;

        Handles.color = Color.yellow;
        using (new Handles.DrawingScope(Matrix4x4.identity))
        {
            visualBoundsHandle.center = bounds.center;
            visualBoundsHandle.size = new Vector3(bounds.size.x, bounds.size.y, 0.01f);

            EditorGUI.BeginChangeCheck();
            visualBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                float newHeight = Mathf.Max(0.1f, visualBoundsHandle.size.y);

                Undo.RecordObject(controller.transform, "Resize Player Visual");
                Undo.RecordObject(controller, "Resize Player Visual");

                controller.SetVisualHeight(newHeight);

                SpriteRenderer updatedRenderer = controller.GetSpriteRenderer();
                if (updatedRenderer != null && updatedRenderer.sprite != null)
                {
                    Bounds updatedBounds = updatedRenderer.bounds;
                    float deltaToKeepFeetGrounded = originalBottomY - updatedBounds.min.y;
                    controller.transform.position += new Vector3(0f, deltaToKeepFeetGrounded, 0f);
                }

                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(controller.transform);
            }
        }
    }

    private void DrawGroundCheckHandle(PlayerController controller)
    {
        Transform groundCheck = controller.GroundCheckTransform;
        if (groundCheck == null)
        {
            return;
        }

        Handles.color = Color.red;

        EditorGUI.BeginChangeCheck();
        Vector3 newWorldPosition = Handles.PositionHandle(groundCheck.position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(new Object[] { controller, groundCheck }, "Move Ground Check");
            controller.SetGroundCheckLocalPosition(controller.transform.InverseTransformPoint(newWorldPosition));
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(groundCheck);
        }

        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(Quaternion.identity, groundCheck.position, controller.GroundCheckRadius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Resize Ground Check");
            controller.GroundCheckRadius = newRadius;
            EditorUtility.SetDirty(controller);
        }
    }

    private void DrawColliderHandle(PlayerController controller)
    {
        BoxCollider2D collider2D = controller.GetPlayerCollider();
        if (collider2D == null)
        {
            return;
        }

        Handles.color = Color.cyan;
        using (new Handles.DrawingScope(collider2D.transform.localToWorldMatrix))
        {
            colliderBoundsHandle.center = new Vector3(collider2D.offset.x, collider2D.offset.y, 0f);
            colliderBoundsHandle.size = new Vector3(collider2D.size.x, collider2D.size.y, 0.01f);

            EditorGUI.BeginChangeCheck();
            colliderBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Resize Player Collider");
                controller.SetColliderShape(
                    new Vector2(colliderBoundsHandle.center.x, colliderBoundsHandle.center.y),
                    new Vector2(colliderBoundsHandle.size.x, colliderBoundsHandle.size.y));
                EditorUtility.SetDirty(controller);
            }
        }
    }

    private void DrawVisualHeightHandle(PlayerController controller)
    {
        SpriteRenderer renderer = controller.GetSpriteRenderer();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Bounds bounds = renderer.bounds;
        Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, controller.transform.position.z);

        Handles.color = Color.yellow;
        EditorGUI.BeginChangeCheck();
        Vector3 newTopCenter = Handles.Slider(topCenter, Vector3.up, HandleUtility.GetHandleSize(topCenter) * 0.14f, Handles.CubeHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            float bottomY = bounds.min.y;
            float newHeight = Mathf.Max(0.1f, newTopCenter.y - bottomY);
            Undo.RecordObject(controller, "Adjust Visual Height");
            controller.SetVisualHeight(newHeight);
            EditorUtility.SetDirty(controller);
        }

        Handles.Label(topCenter + Vector3.right * 0.35f, $"Visual Height: {controller.VisualHeight:F2}");
    }
}
