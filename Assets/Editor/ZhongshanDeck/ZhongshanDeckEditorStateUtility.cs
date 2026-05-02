using UnityEditor;
using UnityEngine;

public static class ZhongshanDeckEditorStateUtility
{
    public const string AssetFolder = "Assets/Resources/Debug";
    public const string AssetPath = AssetFolder + "/ZhongshanDeckToolState.asset";

    public static ZhongshanDeckToolState GetOrCreateStateAsset()
    {
        ZhongshanDeckToolState asset = AssetDatabase.LoadAssetAtPath<ZhongshanDeckToolState>(AssetPath);
        if (asset != null)
        {
            asset.EnsureInitialized();
            return asset;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Debug");
        }

        asset = ScriptableObject.CreateInstance<ZhongshanDeckToolState>();
        asset.EnsureInitialized();
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }
}
