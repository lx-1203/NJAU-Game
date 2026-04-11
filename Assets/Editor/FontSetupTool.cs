using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// 字体初始化工具 - 编辑器菜单
///
/// 【使用方法】
/// 1. 先把你的中文 .ttf 字体文件放到 Assets/Fonts/ 目录下
/// 2. 点击菜单栏 工具/字体管理/一键初始化字体系统
/// 3. 搞定！以后新建界面中文自动显示
/// </summary>
public class FontSetupTool : EditorWindow
{
    private Font selectedFont;
    private TMP_FontAsset existingSDF;

    [MenuItem("工具/字体管理/一键初始化字体系统")]
    public static void ShowWindow()
    {
        var window = GetWindow<FontSetupTool>("字体初始化工具");
        window.minSize = new Vector2(450, 350);
    }

    [MenuItem("工具/字体管理/在场景中创建 FontManager")]
    public static void CreateFontManagerInScene()
    {
        if (Object.FindObjectOfType<FontManager>() != null)
        {
            EditorUtility.DisplayDialog("提示", "场景中已存在 FontManager！", "好的");
            return;
        }

        GameObject obj = new GameObject("FontManager");
        obj.AddComponent<FontManager>();
        Selection.activeGameObject = obj;
        EditorUtility.DisplayDialog("完成", "FontManager 已创建！\n请在 Inspector 中拖入你的中文 SDF 字体。", "好的");
    }

    private void OnGUI()
    {
        GUILayout.Label("中文字体一键初始化", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "本工具帮你一次性完成字体配置：\n" +
            "1. 生成 SDF 字体资源\n" +
            "2. 放入 Resources 目录\n" +
            "3. 配置 TMP 全局 fallback\n" +
            "4. 创建 FontManager\n\n" +
            "之后新建的界面中文自动能显示！",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 第一步：选择字体
        GUILayout.Label("第1步：选择中文 TTF 字体文件", EditorStyles.boldLabel);
        selectedFont = (Font)EditorGUILayout.ObjectField("中文字体 (.ttf)", selectedFont, typeof(Font), false);

        EditorGUILayout.Space(5);

        // 或者直接选已有的 SDF
        GUILayout.Label("已有 SDF 字体？直接选它：", EditorStyles.miniLabel);
        existingSDF = (TMP_FontAsset)EditorGUILayout.ObjectField("SDF 字体资源", existingSDF, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(15);

        // 一键配置
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("一键配置全局中文字体", GUILayout.Height(40)))
        {
            DoSetup();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // 单独操作
        GUILayout.Label("单独操作：", EditorStyles.boldLabel);

        if (GUILayout.Button("仅配置 TMP 全局 Fallback"))
        {
            SetupTMPFallback();
        }

        if (GUILayout.Button("在场景中创建 FontManager 对象"))
        {
            CreateFontManagerInScene();
        }

        if (GUILayout.Button("打开 Fonts 目录"))
        {
            string fontsDir = "Assets/Fonts";
            if (!Directory.Exists(fontsDir))
            {
                Directory.CreateDirectory(fontsDir);
                AssetDatabase.Refresh();
            }
            EditorUtility.RevealInFinder(fontsDir);
        }
    }

    private void DoSetup()
    {
        TMP_FontAsset sdfFont = existingSDF;

        // 如果没有 SDF，检查是否选了 TTF
        if (sdfFont == null && selectedFont == null)
        {
            EditorUtility.DisplayDialog("错误",
                "请先选择一个中文字体文件（TTF）或已有的 SDF 字体资源！\n\n" +
                "如果你还没有中文字体，请先将 .ttf 文件放到 Assets/Fonts/ 目录下。",
                "好的");
            return;
        }

        // 如果选了 TTF 但没有 SDF，提示手动生成
        if (sdfFont == null && selectedFont != null)
        {
            bool generate = EditorUtility.DisplayDialog("生成 SDF 字体",
                "需要先将 TTF 字体转换为 TMP SDF 字体资源。\n\n" +
                "请按以下步骤操作：\n" +
                "1. 菜单 Window → TextMeshPro → Font Asset Creator\n" +
                "2. Source Font File 选择你的中文字体\n" +
                "3. Atlas Resolution 设为 4096×4096\n" +
                "4. Character Set 选 Custom Characters\n" +
                "5. 粘贴你需要的中文字符\n" +
                "6. 点击 Generate Font Atlas\n" +
                "7. 保存到 Assets/Resources/Fonts/ 目录\n\n" +
                "生成完成后，再回到本工具选择 SDF 字体。\n\n" +
                "是否现在打开 Font Asset Creator？",
                "打开", "取消");

            if (generate)
            {
                // 确保目录存在
                EnsureDirectories();
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Font Asset Creator");
            }
            return;
        }

        // 有 SDF 字体了，开始配置
        EnsureDirectories();

        // 复制到 Resources/Fonts（如果不在的话）
        string sdfPath = AssetDatabase.GetAssetPath(sdfFont);
        if (!sdfPath.Contains("Resources"))
        {
            string destPath = "Assets/Resources/Fonts/" + Path.GetFileName(sdfPath);
            if (!File.Exists(destPath))
            {
                AssetDatabase.CopyAsset(sdfPath, destPath);
                AssetDatabase.Refresh();
                sdfFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destPath);
                Debug.Log($"[FontSetupTool] 已复制字体到 {destPath}");
            }
        }

        // 配置 TMP 全局 fallback
        SetupTMPFallbackWithFont(sdfFont);

        // 在场景中创建 FontManager（如果没有）
        FontManager fm = Object.FindObjectOfType<FontManager>();
        if (fm == null)
        {
            GameObject obj = new GameObject("FontManager");
            fm = obj.AddComponent<FontManager>();
            Undo.RegisterCreatedObjectUndo(obj, "Create FontManager");
        }
        fm.chineseFontAsset = sdfFont;
        EditorUtility.SetDirty(fm);

        EditorUtility.DisplayDialog("配置完成！",
            "中文字体已全局配置完成：\n\n" +
            $"✅ SDF 字体：{sdfFont.name}\n" +
            "✅ TMP 全局 Fallback 已设置\n" +
            "✅ FontManager 已创建\n" +
            "✅ 字体已放入 Resources\n\n" +
            "以后新建界面的中文文字应该都能正常显示了！\n\n" +
            "提示：请记住在每个场景中都要有 FontManager 对象，\n" +
            "或者把它放在一个 DontDestroyOnLoad 的对象上。",
            "好的");
    }

    private void SetupTMPFallback()
    {
        TMP_FontAsset font = existingSDF;
        if (font == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个 SDF 字体资源！", "好的");
            return;
        }
        SetupTMPFallbackWithFont(font);
        EditorUtility.DisplayDialog("完成", $"已将 {font.name} 设为 TMP 全局 Fallback 字体。", "好的");
    }

    private void SetupTMPFallbackWithFont(TMP_FontAsset sdfFont)
    {
        // 获取 TMP 默认字体并添加 fallback
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
        {
            if (defaultFont.fallbackFontAssetTable == null)
            {
                defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }
            if (!defaultFont.fallbackFontAssetTable.Contains(sdfFont))
            {
                defaultFont.fallbackFontAssetTable.Add(sdfFont);
                EditorUtility.SetDirty(defaultFont);
                Debug.Log($"[FontSetupTool] 已将 {sdfFont.name} 添加到默认字体的 fallback 列表");
            }
        }

        // 同时设置 TMP Settings 的全局 fallback
        TMP_Settings settings = TMP_Settings.instance;
        if (settings != null)
        {
            var fallbackField = typeof(TMP_Settings).GetField("m_fallbackFontAssets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fallbackField != null)
            {
                var list = fallbackField.GetValue(settings) as System.Collections.Generic.List<TMP_FontAsset>;
                if (list == null)
                {
                    list = new System.Collections.Generic.List<TMP_FontAsset>();
                }
                if (!list.Contains(sdfFont))
                {
                    list.Add(sdfFont);
                    fallbackField.SetValue(settings, list);
                    EditorUtility.SetDirty(settings);
                    Debug.Log($"[FontSetupTool] 已将 {sdfFont.name} 添加到 TMP Settings 全局 fallback");
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    private void EnsureDirectories()
    {
        if (!Directory.Exists("Assets/Fonts"))
        {
            Directory.CreateDirectory("Assets/Fonts");
        }
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
        }
        if (!Directory.Exists("Assets/Resources/Fonts"))
        {
            Directory.CreateDirectory("Assets/Resources/Fonts");
        }
        AssetDatabase.Refresh();
    }
}
