using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 编辑器工具：一键生成 TitleScreen 场景
/// 菜单路径：Tools → 创建 TitleScreen 场景
/// </summary>
public class TitleScreenSceneCreator
{
    [MenuItem("Tools/创建 TitleScreen 场景")]
    public static void CreateTitleScreenScene()
    {
        // 创建新场景
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 删除默认方向光（标题页不需要）
        Light[] lights = Object.FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            Object.DestroyImmediate(light.gameObject);
        }

        // 设置摄像机背景色为纯黑
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        // 创建 EventSystem（UI 点击检测必需）
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // 创建 TitleScreenManager（Canvas + 所有 UI 由脚本自动生成）
        GameObject titleScreenGO = new GameObject("TitleScreenCanvas");
        titleScreenGO.AddComponent<TitleScreenManager>();

        // 保存场景
        string scenePath = "Assets/Scenes/TitleScreen.unity";
        bool saved = EditorSceneManager.SaveScene(newScene, scenePath);

        if (saved)
        {
            Debug.Log($"[TitleScreen] 场景已创建并保存到：{scenePath}");
            Debug.Log("[TitleScreen] 请在 File → Build Settings 中将 TitleScreen 添加为第一个场景（Index 0）");

            // 选中创建的对象
            Selection.activeGameObject = titleScreenGO;
        }
        else
        {
            Debug.LogError("[TitleScreen] 场景保存失败！");
        }
    }
}
