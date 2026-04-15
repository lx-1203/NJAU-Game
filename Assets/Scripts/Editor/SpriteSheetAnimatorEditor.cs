using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// SpriteSheetAnimator 的 Inspector 辅助编辑器
/// 提供快速配置动画的按钮
/// </summary>
[UnityEditor.CustomEditor(typeof(SpriteSheetAnimator))]
public class SpriteSheetAnimatorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        SpriteSheetAnimator animator = (SpriteSheetAnimator)target;

        // 自动检测按钮
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自动配置", UnityEditor.EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从 SpriteRenderer 加载精灵", GUILayout.Height(30)))
        {
            AutoConfigureFromSpriteRenderer(animator);
        }
        if (GUILayout.Button("从切片自动配置 Idle", GUILayout.Height(30)))
        {
            AutoConfigureFromSlices(animator, "Idle");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 手动配置说明
        EditorGUILayout.HelpBox(
            "推荐做法：\n" +
            "1. 先在 Project 选中 PlayerWalkSprites.png\n" +
            "2. 设置 Sprite Mode = Multiple\n" +
            "3. Sprite Editor → Slice → Type: Grid by Cell Size\n" +
            "4. 设置 Cell Size（如 128x128）\n" +
            "5. 切片后，展开图片点击每个子精灵，右键 Rename\n" +
            "6. 把精灵拖到下方 Animations 列表中对应动画名下",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("动画配置", UnityEditor.EditorStyles.boldLabel);

        // 显示默认 Inspector
        DrawDefaultInspector();
    }

    private void AutoConfigureFromSpriteRenderer(SpriteSheetAnimator animator)
    {
        SpriteRenderer sr = animator.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // 尝试获取 sprite 的名称
            string spriteName = sr.sprite.name;
            Debug.Log($"当前精灵名称: {spriteName}");
            UnityEditor.Selection.activeObject = sr.sprite;
        }
    }

    private void AutoConfigureFromSlices(SpriteSheetAnimator animator, string clipName)
    {
        // 查找切片后的子精灵
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources" });

        var sprites = guids
            .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .Where(s => s.name.ToLower().Contains("walk") || s.name.ToLower().Contains("idle"))
            .ToArray();

        if (sprites.Length > 0)
        {
            Debug.Log($"找到 {sprites.Length} 个可能的动画精灵: {string.Join(", ", sprites.Select(s => s.name))}");
            UnityEditor.Selection.objects = sprites;
        }
        else
        {
            Debug.LogWarning("未找到任何子精灵！请先在 Sprite Editor 中切片图片。");
        }
    }
}
