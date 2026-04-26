using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteSheetAnimator))]
public class SpriteSheetAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpriteSheetAnimator animator = (SpriteSheetAnimator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Focus Current Sprite", GUILayout.Height(30)))
        {
            FocusCurrentSprite(animator);
        }

        if (GUILayout.Button("Find Frame Sprites", GUILayout.Height(30)))
        {
            FindFrameSprites();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Character animation frames are now loaded from Resources folders:\n" +
            "Assets/Resources/PlayerIdleFrames\n" +
            "Assets/Resources/PlayerWalkFrames\n" +
            "Assets/Resources/PlayerJumpFrames\n" +
            "Runtime loading uses LoadFromResources for each folder.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Clips", EditorStyles.boldLabel);
        DrawDefaultInspector();
    }

    private static void FocusCurrentSprite(SpriteSheetAnimator animator)
    {
        SpriteRenderer spriteRenderer = animator.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Debug.Log($"Current sprite: {spriteRenderer.sprite.name}");
            Selection.activeObject = spriteRenderer.sprite;
        }
    }

    private static void FindFrameSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources" });

        Sprite[] sprites = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(sprite => sprite != null)
            .Where(sprite =>
                sprite.name.ToLower().Contains("idle") ||
                sprite.name.ToLower().Contains("walk") ||
                sprite.name.ToLower().Contains("jump"))
            .ToArray();

        if (sprites.Length > 0)
        {
            Debug.Log($"Found {sprites.Length} animation frame sprites.");
            Selection.objects = sprites;
        }
        else
        {
            Debug.LogWarning("No animation frame sprites were found under Assets/Resources.");
        }
    }
}
