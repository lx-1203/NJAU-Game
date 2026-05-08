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
    public bool useStartupTimeOverride;
    public int semesterRoundCount = 5;
    public int startupYear = 1;
    public int startupSemester = 1;
    public int startupRound = 1;
    public int startupStudy = 10;
    public int startupCharm = 5;
    public int startupPhysique = 8;
    public int startupLeadership = 3;
    public int startupStress = 20;
    public int startupMood = 70;
    public int startupDarkness;
    public int startupGuilt;
    public int startupLuck = 50;
    public int startupMoney = 8000;
    public int startupActionPoints = 20;

    [Header("Debug Controls")]
    public int stepIndex = 1;

    [Header("Snapshots")]
    public List<ZhongshanDeckSnapshotEntry> snapshots = new List<ZhongshanDeckSnapshotEntry>();

    [Header("Authored Events")]
    public List<ZhongshanDeckEventEntry> authoredEvents = new List<ZhongshanDeckEventEntry>();

    [Header("Monthly News Overrides")]
    public List<ZhongshanDeckNewsRoundEntry> monthlyNewsOverrides = new List<ZhongshanDeckNewsRoundEntry>();

    [Header("Title Authored Content")]
    public ZhongshanDeckTitleContent titleContent = new ZhongshanDeckTitleContent();

    [Header("Save Load Layout Content")]
    public ZhongshanDeckSaveLoadContent saveLoadContent = new ZhongshanDeckSaveLoadContent();

    public void EnsureInitialized()
    {
        defaultPlayerName ??= "Teat";
        defaultPlayerMajor ??= "\u751f\u7269\u79d1\u5b66\u4e13\u4e1a";
        snapshots ??= new List<ZhongshanDeckSnapshotEntry>();
        authoredEvents ??= new List<ZhongshanDeckEventEntry>();
        monthlyNewsOverrides ??= new List<ZhongshanDeckNewsRoundEntry>();
        titleContent ??= new ZhongshanDeckTitleContent();
        titleContent.EnsureInitialized();
        saveLoadContent ??= new ZhongshanDeckSaveLoadContent();
        saveLoadContent.EnsureInitialized();
        stepIndex = Mathf.Clamp(stepIndex, 0, 3);
        defaultPlayerGender = Mathf.Clamp(defaultPlayerGender, 0, 1);
        semesterRoundCount = Mathf.Clamp(semesterRoundCount, 3, 12);
        startupYear = Mathf.Clamp(startupYear, 1, 4);
        startupSemester = Mathf.Clamp(startupSemester, 1, 2);
        startupRound = Mathf.Clamp(startupRound, 1, semesterRoundCount);
        startupStudy = Mathf.Clamp(startupStudy, 0, 999);
        startupCharm = Mathf.Clamp(startupCharm, 0, 999);
        startupPhysique = Mathf.Clamp(startupPhysique, 0, 999);
        startupLeadership = Mathf.Clamp(startupLeadership, 0, 999);
        startupStress = Mathf.Clamp(startupStress, 0, PlayerAttributes.MaxStatusValue);
        startupMood = Mathf.Clamp(startupMood, 0, PlayerAttributes.MaxStatusValue);
        startupDarkness = Mathf.Clamp(startupDarkness, 0, 999);
        startupGuilt = Mathf.Clamp(startupGuilt, 0, PlayerAttributes.MaxStatusValue);
        startupLuck = Mathf.Clamp(startupLuck, 0, PlayerAttributes.MaxStatusValue);
        startupActionPoints = Mathf.Clamp(startupActionPoints, 1, 999);
    }
}

[Serializable]
public class ZhongshanDeckSnapshotEntry
{
    public string name;
    [TextArea(3, 12)]
    public string json;
}

[Serializable]
public class ZhongshanDeckEventEntry
{
    public string eventId;
    public string title;
    [TextArea(8, 30)]
    public string json;
}

[Serializable]
public class ZhongshanDeckNewsRoundEntry
{
    public int year = 1;
    public int semester = 1;
    public int round = 1;
    public List<NewsItem> items = new List<NewsItem>();
}
