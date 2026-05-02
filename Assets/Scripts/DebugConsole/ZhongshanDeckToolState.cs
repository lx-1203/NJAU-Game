using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZhongshanDeckToolState", menuName = "钟山台/Tool State")]
public class ZhongshanDeckToolState : ScriptableObject
{
    [Header("Startup Flow")]
    public bool skipSplashLogo;
    public bool skipCharacterCreation;
    public bool skipOpeningStory;
    public bool skipTitleScreen;
    public string defaultPlayerName = "Teat";
    public int defaultPlayerGender = 1;
    public string defaultPlayerMajor = "\u751f\u7269\u79d1\u5b66\u4e13\u4e1a";

    [Header("Debug Controls")]
    public int stepIndex = 1;

    [Header("Snapshots")]
    public List<ZhongshanDeckSnapshotEntry> snapshots = new List<ZhongshanDeckSnapshotEntry>();

    public void EnsureInitialized()
    {
        defaultPlayerName ??= "Teat";
        defaultPlayerMajor ??= "\u751f\u7269\u79d1\u5b66\u4e13\u4e1a";
        snapshots ??= new List<ZhongshanDeckSnapshotEntry>();
        stepIndex = Mathf.Clamp(stepIndex, 0, 3);
        defaultPlayerGender = Mathf.Clamp(defaultPlayerGender, 0, 1);
    }
}

[Serializable]
public class ZhongshanDeckSnapshotEntry
{
    public string name;
    [TextArea(3, 12)]
    public string json;
}
