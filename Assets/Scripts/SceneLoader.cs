using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 异步场景加载工具类
/// 提供静态方法供全局调用，通过加载界面过渡到目标场景
/// 整合了远程版本的退出游戏功能
/// </summary>
public static class SceneLoader
{
    // 加载界面场景名称
    private const string LOADING_SCENE_NAME = "LoadingScreen";

    // 目标场景名称（供 LoadingScreenManager 读取）
    public static string TargetSceneName { get; private set; }

    /// <summary>
    /// 通过加载界面切换到目标场景
    /// 异步加载 LoadingScreen 场景，避免黑屏卡顿
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] 目标场景名称不能为空！");
            return;
        }

        // 记录目标场景名称
        TargetSceneName = sceneName;
        Debug.Log($"[SceneLoader] 准备加载场景: {sceneName}，进入加载界面...");

        // 异步加载 LoadingScreen 场景，避免同步加载导致的黑屏
        SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
    }

    /// <summary>
    /// 直接加载场景（无过渡，用于简单场景切换）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public static void LoadSceneDirect(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] 目标场景名称不能为空！");
            return;
        }

        Debug.Log($"[SceneLoader] 直接加载场景: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 退出游戏（来自远程版本的功能）
    /// </summary>
    public static void QuitGame()
    {
        Debug.Log("[SceneLoader] 退出游戏");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
