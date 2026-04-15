#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 自动设置 Play Mode 起始场景为 SplashScreen
/// 确保 Ctrl+P 始终从 SplashScreen 开始，无论当前打开的是哪个场景
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartSceneSetter
{
    private const string SplashScenePath = "Assets/Scenes/SplashScreen.unity";

    static PlayModeStartSceneSetter()
    {
        var splashScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SplashScenePath);
        if (splashScene != null)
        {
            if (EditorSceneManager.playModeStartScene != splashScene)
            {
                EditorSceneManager.playModeStartScene = splashScene;
                Debug.Log($"[PlayModeStartScene] 已设置起始场景: {SplashScenePath}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayModeStartScene] 找不到场景: {SplashScenePath}，请检查路径是否正确");
        }
    }
}
#endif
