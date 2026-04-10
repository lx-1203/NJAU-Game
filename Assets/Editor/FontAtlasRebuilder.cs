using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 一键重新生成高质量 TMP 中文字体 Atlas
/// 菜单: Tools → 重新生成字体 Atlas (高质量)
/// </summary>
public class FontAtlasRebuilder : EditorWindow
{
    [MenuItem("Tools/重新生成字体 Atlas (高质量)")]
    public static void RebuildFontAtlas()
    {
        // 收集项目中实际使用的所有中文字符
        string allChars = CollectUsedCharacters();

        // 源字体路径
        string fontPath = "Assets/Fonts/STXINWEI.TTF";
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("错误",
                $"找不到源字体文件: {fontPath}\n请确保 STXINWEI.TTF 在 Assets/Fonts/ 目录下。", "确定");
            return;
        }

        // 重新生成两个字体资源
        RebuildSingleFont("Assets/Fonts/STXINWEI SDF.asset", sourceFont, allChars);
        RebuildSingleFont("Assets/Fonts/STXINWEI SDF 1.asset", sourceFont, allChars);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            "字体 Atlas 已重新生成！\n\n" +
            "• Atlas 尺寸: 4096×4096\n" +
            "• Sampling Point Size: 90\n" +
            "• Padding: 9\n" +
            $"• 包含字符数: {allChars.Length}\n\n" +
            "请重新打开 LoadingScreen 场景查看效果。", "确定");
    }

    private static void RebuildSingleFont(string fontAssetPath, Font sourceFont, string characters)
    {
        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), fontAssetPath);
        if (File.Exists(absolutePath))
        {
            AssetDatabase.DeleteAsset(fontAssetPath);
            AssetDatabase.Refresh();
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            Debug.LogError($"创建 TMP_FontAsset 失败: {fontAssetPath}");
            return;
        }

        fontAsset.name = Path.GetFileNameWithoutExtension(fontAssetPath);
        AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

        Texture2D atlasTexture = null;
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            atlasTexture = fontAsset.atlasTextures[0];
        }

        if (atlasTexture != null)
        {
            atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        bool success = fontAsset.TryAddCharacters(characters, out string missingChars, true);

        if (!string.IsNullOrEmpty(missingChars))
        {
            Debug.LogWarning($"[{fontAssetPath}] 部分字符未能添加 ({missingChars.Length} 个)，这些字符可能不在字体文件中。");
        }

        EnsureFontMaterial(fontAsset, fontAssetPath);

        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.material != null)
        {
            EditorUtility.SetDirty(fontAsset.material);
        }
        if (atlasTexture != null)
        {
            EditorUtility.SetDirty(atlasTexture);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(fontAssetPath, ImportAssetOptions.ForceUpdate);
        fontAsset.ReadFontAssetDefinition();

        Debug.Log($"完成重新生成: {fontAssetPath} - 成功添加 {(success ? characters.Length : characters.Length - (missingChars?.Length ?? 0))} 个字符");
    }

    private static void EnsureFontMaterial(TMP_FontAsset fontAsset, string fontAssetPath)
    {
        if (fontAsset == null)
        {
            return;
        }

        Material material = fontAsset.material;
        if (material == null)
        {
            Shader shader = Shader.Find("TextMeshPro/Distance Field");
            material = new Material(shader);
            material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            fontAsset.material = material;
        }

        Texture2D atlasTexture = null;
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            atlasTexture = fontAsset.atlasTextures[0];
        }

        if (atlasTexture != null)
        {
            material.mainTexture = atlasTexture;
            material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);
            material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasTexture.width);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasTexture.height);
            material.SetFloat(ShaderUtilities.ID_GradientScale, fontAsset.atlasPadding + 1f);
        }

        EditorUtility.SetDirty(material);
        AssetDatabase.ImportAsset(fontAssetPath, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// 收集项目中所有场景、脚本中使用到的中文字符
    /// </summary>
    private static string CollectUsedCharacters()
    {
        HashSet<char> chars = new HashSet<char>();

        // 基础常用字符集 - 包含 ASCII + 常用中文标点
        string basicChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
                           "!@#$%^&*()_+-=[]{}|;':\",./<>?`~ " +
                           "。，、；：？！\u201C\u2018\u2019\u201D【】（）—…《》￥·";

        foreach (char c in basicChars)
            chars.Add(c);

        // 从场景文件中提取中文字符
        string[] sceneFiles = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);
        foreach (string file in sceneFiles)
        {
            ExtractChineseChars(File.ReadAllText(file), chars);
        }

        // 从 C# 脚本中提取中文字符（字符串中的中文）
        string[] csFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        foreach (string file in csFiles)
        {
            ExtractChineseChars(File.ReadAllText(file), chars);
        }

        // 从 ScriptableObject (.asset) 中提取
        string[] assetFiles = Directory.GetFiles("Assets/UI", "*.asset", SearchOption.AllDirectories);
        foreach (string file in assetFiles)
        {
            ExtractChineseChars(File.ReadAllText(file), chars);
        }

        // 添加额外的常用中文字符（预防性），确保常用汉字都包含
        string commonChinese =
            "的一是不了人我在有他这为之大来以个中上们到说时地也就出会可对生能而子那得于着下自" +
            "之后作年前学成方都要用没把种好很行多日新同小被过两事长加将已开进所动关看门经起最" +
            "什从无想现知回又当面公心问意走些让点世如但老家然法叫向第等部相全间它本外面高理天" +
            "三手与别海书女因才入道只正更体真系明该度比声特手情信内者利此感打件话金力发美月" +
            "合处合使安啊身路吗气各水通原觉并直白何至民管又话定变已果少给常色叫教风已入每" +
            // 项目专用字符
            "钟山下加载中正在翻找课本食堂阿姨打饭图书馆占座铺床单辅导员点名快递小哥派件" +
            "合理安排时间学习娱乐两不误多和室友交流大学生活更精彩别忘记按时吃饭身体革命" +
            "本钱适度游戏益脑沉迷伤跳过小贴士版";

        foreach (char c in commonChinese)
            chars.Add(c);

        // 转为排序后的字符串
        var sortedChars = chars.OrderBy(c => c).ToArray();
        return new string(sortedChars);
    }

    private static void ExtractChineseChars(string text, HashSet<char> chars)
    {
        // 解码 Unity YAML 中的 Unicode 转义序列 (如 \u949F)
        System.Text.RegularExpressions.Regex regex =
            new System.Text.RegularExpressions.Regex(@"\\u([0-9a-fA-F]{4})");

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(text))
        {
            int code = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            chars.Add((char)code);
        }

        // 直接提取文本中的中文字符
        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) // CJK Unified Ideographs
                chars.Add(c);
            if (c >= 0x3000 && c <= 0x303F) // CJK Symbols and Punctuation
                chars.Add(c);
            if (c >= 0xFF00 && c <= 0xFFEF) // Fullwidth Forms
                chars.Add(c);
        }
    }
}
