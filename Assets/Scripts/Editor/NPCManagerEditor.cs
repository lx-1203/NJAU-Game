using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Anchor Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use LocationSceneController preview location plus NPCManager preview time slot to show editable NPC anchors in Scene view. By default, the preview time slot now follows the startup action points so the editor preview matches a fresh game start. Move or scale those NPC objects directly, and runtime NPC placement will reuse them.",
            MessageType.Info);

        NPCManager manager = (NPCManager)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Anchor Preview"))
            {
                manager.RefreshEditorSceneAnchors();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Select Anchor Root"))
            {
                Transform root = manager.GetSceneAnchorRoot();
                if (root != null)
                {
                    Selection.activeTransform = root;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }
        }
    }
}
