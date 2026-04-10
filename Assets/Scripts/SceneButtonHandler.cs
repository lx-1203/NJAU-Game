using UnityEngine;

/// <summary>
/// 场景按钮事件处理器（MonoBehaviour 组件）
/// 挂载到 GameObject 上，供 UI 按钮 OnClick 事件绑定
/// 内部调用 SceneLoader 静态方法，保持架构统一
/// </summary>
public class SceneButtonHandler : MonoBehaviour
{
    [Header("场景名称配置")]
    [Tooltip("目标游戏场景名称")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Tooltip("开始菜单场景名称")]
    [SerializeField] private string startMenuSceneName = "SampleScene";

    [Header("加载方式")]
    [Tooltip("是否使用加载界面过渡（勾选=异步加载+过渡动画，不勾选=直接切换）")]
    [SerializeField] private bool useLoadingScreen = true;

    /// <summary>
    /// 加载游戏场景 - 绑定到"开始游戏"按钮
    /// </summary>
    public void LoadGameScene()
    {
        if (useLoadingScreen)
        {
            SceneLoader.LoadScene(gameSceneName);
        }
        else
        {
            SceneLoader.LoadSceneDirect(gameSceneName);
        }
    }

    /// <summary>
    /// 返回开始菜单 - 绑定到"返回主菜单"按钮
    /// </summary>
    public void LoadStartMenu()
    {
        if (useLoadingScreen)
        {
            SceneLoader.LoadScene(startMenuSceneName);
        }
        else
        {
            SceneLoader.LoadSceneDirect(startMenuSceneName);
        }
    }

    /// <summary>
    /// 退出游戏 - 绑定到"退出游戏"按钮
    /// </summary>
    public void QuitGame()
    {
        SceneLoader.QuitGame();
    }
}
