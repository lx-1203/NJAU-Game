using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景加载器 - 用于从开始菜单跳转到游戏场景
/// 挂载到 StartMenu 场景中的按钮或管理器对象上
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// 加载游戏场景（横版2D游戏）
    /// 可绑定到 UI 按钮的 OnClick 事件
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// 返回开始菜单
    /// </summary>
    public void LoadStartMenu()
    {
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
