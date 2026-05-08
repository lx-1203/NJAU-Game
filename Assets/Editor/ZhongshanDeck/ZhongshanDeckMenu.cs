using UnityEditor;

public static class ZhongshanDeckMenu
{
    private const string Root = "钟山台/调试台/";
    private const string ModuleRoot = Root + "模块/";
    private const string PreviewRoot = Root + "编辑器界面/";

    [MenuItem(Root + "总控窗口", false, 0)]
    public static void OpenWindow()
    {
        ZhongshanDeckWindow.Open();
    }

    [MenuItem(Root + "剧情与事件编辑器", false, 5)]
    public static void OpenCreatorToolkit()
    {
        EditorApplication.ExecuteMenuItem("钟山台/造物主 (Creator Toolkit)/剧情与事件编辑器");
    }

    [MenuItem(Root + "游戏内钟山台/打开", false, 20)]
    public static void OpenInGameConsole()
    {
        if (DebugConsoleManager.Instance != null)
        {
            DebugConsoleManager.Instance.Open();
        }
        else
        {
            EditorUtility.DisplayDialog("钟山台", "当前不在可用的 Play 模式，或 DebugConsoleManager 尚未初始化。", "确定");
        }
    }

    [MenuItem(Root + "游戏内钟山台/打开", true)]
    public static bool ValidateOpenInGameConsole()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem(Root + "游戏内钟山台/关闭", false, 21)]
    public static void CloseInGameConsole()
    {
        DebugConsoleManager.Instance?.Close();
    }

    [MenuItem(Root + "游戏内钟山台/关闭", true)]
    public static bool ValidateCloseInGameConsole()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem(Root + "启动流/快速开局", false, 40)]
    public static void ApplyQuickStart()
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        StartupFlowSettings.ApplyQuickStartPreset();
    }

    [MenuItem(Root + "启动流/跳过开屏", false, 41)]
    public static void ToggleSkipSplash()
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        StartupFlowSettings.SkipSplashLogo = !StartupFlowSettings.SkipSplashLogo;
    }

    [MenuItem(Root + "启动流/跳过开屏", true)]
    public static bool ValidateToggleSkipSplash()
    {
        Menu.SetChecked(Root + "启动流/跳过开屏", StartupFlowSettings.SkipSplashLogo);
        return true;
    }

    [MenuItem(Root + "启动流/跳过建角", false, 42)]
    public static void ToggleSkipCharacterCreation()
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        StartupFlowSettings.SkipCharacterCreation = !StartupFlowSettings.SkipCharacterCreation;
    }

    [MenuItem(Root + "启动流/跳过建角", true)]
    public static bool ValidateToggleSkipCharacterCreation()
    {
        Menu.SetChecked(Root + "启动流/跳过建角", StartupFlowSettings.SkipCharacterCreation);
        return true;
    }

    [MenuItem(Root + "启动流/跳过开场", false, 43)]
    public static void ToggleSkipOpeningStory()
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        StartupFlowSettings.SkipOpeningStory = !StartupFlowSettings.SkipOpeningStory;
    }

    [MenuItem(Root + "启动流/跳过开场", true)]
    public static bool ValidateToggleSkipOpeningStory()
    {
        Menu.SetChecked(Root + "启动流/跳过开场", StartupFlowSettings.SkipOpeningStory);
        return true;
    }

    [MenuItem(Root + "启动流/跳过首页", false, 44)]
    public static void ToggleSkipTitleScreen()
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        StartupFlowSettings.SkipTitleScreen = !StartupFlowSettings.SkipTitleScreen;
    }

    [MenuItem(Root + "启动流/跳过首页", true)]
    public static bool ValidateToggleSkipTitleScreen()
    {
        Menu.SetChecked(Root + "启动流/跳过首页", StartupFlowSettings.SkipTitleScreen);
        return true;
    }

    [MenuItem(ModuleRoot + "总览", false, 60)]
    public static void OpenOverviewTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Overview);

    [MenuItem(ModuleRoot + "场景跳转", false, 61)]
    public static void OpenSceneJumpTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.SceneJump);

    [MenuItem(ModuleRoot + "属性", false, 62)]
    public static void OpenAttributesTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Attributes);

    [MenuItem(ModuleRoot + "时间", false, 63)]
    public static void OpenTimeTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Time);

    [MenuItem(ModuleRoot + "内容", false, 64)]
    public static void OpenContentTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Content);

    [MenuItem(ModuleRoot + "新闻", false, 65)]
    public static void OpenNewsTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.News);

    [MenuItem(ModuleRoot + "结局", false, 66)]
    public static void OpenEndingTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Endings);

    [MenuItem(ModuleRoot + "事件", false, 67)]
    public static void OpenEventTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Events);

    [MenuItem(ModuleRoot + "NPC", false, 68)]
    public static void OpenNPCTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.NPC);

    [MenuItem(ModuleRoot + "经济", false, 69)]
    public static void OpenEconomyTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Economy);

    [MenuItem(ModuleRoot + "公式", false, 70)]
    public static void OpenFormulaTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Formula);

    [MenuItem(ModuleRoot + "快照", false, 71)]
    public static void OpenSnapshotTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Snapshots);

    [MenuItem(ModuleRoot + "日志", false, 72)]
    public static void OpenLogTab() => ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Logs);

    [MenuItem(PreviewRoot + "标题页真实预览", false, 90)]
    public static void OpenTitleScenePreview()
    {
        ZhongshanDeckWindow.OpenTitleScenePreview();
    }

    [MenuItem(PreviewRoot + "存档界面真实预览", false, 91)]
    public static void OpenSaveLoadScenePreview()
    {
        ZhongshanDeckWindow.OpenSaveLoadScenePreview();
    }

    [MenuItem(PreviewRoot + "标题内容编辑", false, 92)]
    public static void OpenContentEditor()
    {
        ZhongshanDeckWindow.Open(ZhongshanDeckWindow.Tab.Content);
    }
}
