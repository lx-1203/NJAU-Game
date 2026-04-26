using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 浠诲姟UI绠＄悊鍣?/// 璐熻矗鍙充笂瑙掗€氱煡寮圭獥銆佸彸渚т换鍔¤拷韪€佷换鍔″畬鎴愬脊绐?/// </summary>
public class MissionUI : MonoBehaviour
{
    public static MissionUI Instance { get; private set; }

    private Canvas notificationCanvas;
    private Canvas trackerCanvas;
    private GameObject trackerPanel;
    private Transform trackerContent;
    private Dictionary<string, GameObject> trackerItems = new Dictionary<string, GameObject>();

    private Queue<MissionNotification> notificationQueue = new Queue<MissionNotification>();
    private bool isShowingNotification = false;
    private bool isSubscribed = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UIFlowGuard.EnsureEventSystem();
        CreateNotificationCanvas();
        CreateTrackerCanvas();
        SubscribeToEvents();
        RebuildTrackerFromState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionUnlocked += OnMissionUnlocked;
            MissionSystem.Instance.OnMissionAccepted += OnMissionAccepted;
            MissionSystem.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
            MissionSystem.Instance.OnMissionCompleted += OnMissionCompleted;
            MissionSystem.Instance.OnMissionFailed += OnMissionFailed;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted += OnSaveLoaded;
        }

        isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribed) return;

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionUnlocked -= OnMissionUnlocked;
            MissionSystem.Instance.OnMissionAccepted -= OnMissionAccepted;
            MissionSystem.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
            MissionSystem.Instance.OnMissionCompleted -= OnMissionCompleted;
            MissionSystem.Instance.OnMissionFailed -= OnMissionFailed;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted -= OnSaveLoaded;
        }

        isSubscribed = false;
    }

    /// <summary>
    /// 鍒涘缓閫氱煡Canvas锛堝彸涓婅寮圭獥锛?    /// </summary>
    private void CreateNotificationCanvas()
    {
        GameObject canvasObj = new GameObject("MissionNotificationCanvas");
        canvasObj.transform.SetParent(transform);
        notificationCanvas = canvasObj.AddComponent<Canvas>();
        notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        notificationCanvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.None;
    }

    /// <summary>
    /// 鍒涘缓杩借釜Canvas锛堝彸渚т换鍔″垪琛級
    /// </summary>
    private void CreateTrackerCanvas()
    {
        GameObject canvasObj = new GameObject("MissionTrackerCanvas");
        canvasObj.transform.SetParent(transform);
        trackerCanvas = canvasObj.AddComponent<Canvas>();
        trackerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        trackerCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.None;

        // 鍒涘缓杩借釜闈㈡澘
        trackerPanel = new GameObject("TrackerPanel");
        trackerPanel.transform.SetParent(trackerCanvas.transform, false);

        RectTransform panelRect = trackerPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0.5f);
        panelRect.anchorMax = new Vector2(1, 0.5f);
        panelRect.pivot = new Vector2(1, 0.5f);
        panelRect.anchoredPosition = new Vector2(-20, 0);
        panelRect.sizeDelta = new Vector2(350, 600);

        Image panelBg = trackerPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);

        // 鏍囬
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(trackerPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 40);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "杩涜涓殑浠诲姟";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        if (FontManager.Instance != null)
        {
            titleText.font = FontManager.Instance.ChineseFont;
        }

        // ScrollView
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(trackerPanel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -60);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        trackerContent = contentObj.transform;
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        trackerPanel.SetActive(false);
    }

    /// <summary>
    /// 鏄剧ず浠诲姟瑙ｉ攣閫氱煡
    /// </summary>
    private void OnMissionUnlocked(MissionDefinition mission)
    {
        if (mission.autoAccept) return; // 鑷姩鎺ュ彇鐨勪换鍔′笉鏄剧ず瑙ｉ攣閫氱煡

        notificationQueue.Enqueue(new MissionNotification
        {
            title = "New Mission",
            message = mission.missionName,
            color = new Color(0.3f, 0.6f, 1f),
            duration = 3f
        });

        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }

    /// <summary>
    /// 浠诲姟鎺ュ彇
    /// </summary>
    private void OnMissionAccepted(MissionDefinition mission)
    {
        notificationQueue.Enqueue(new MissionNotification
        {
            title = "浠诲姟鎺ュ彇",
            message = mission.missionName,
            color = new Color(0.2f, 0.8f, 0.2f),
            duration = 2.5f
        });

        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }

        AddTrackerItem(mission);
    }

    /// <summary>
    /// 鐩爣鏇存柊
    /// </summary>
    private void OnObjectiveUpdated(MissionDefinition mission, MissionObjective objective)
    {
        UpdateTrackerItem(mission);
    }

    /// <summary>
    /// 浠诲姟瀹屾垚
    /// </summary>
    private void OnMissionCompleted(MissionDefinition mission)
    {
        RemoveTrackerItem(mission.missionId);
        ShowCompletionPopup(mission);
    }

    /// <summary>
    /// 浠诲姟澶辫触
    /// </summary>
    private void OnMissionFailed(MissionDefinition mission)
    {
        RemoveTrackerItem(mission.missionId);

        notificationQueue.Enqueue(new MissionNotification
        {
            title = "浠诲姟澶辫触",
            message = mission.missionName,
            color = new Color(0.8f, 0.2f, 0.2f),
            duration = 3f
        });

        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }

    private void OnSaveLoaded(int slot)
    {
        RebuildTrackerFromState();
    }

    /// <summary>
    /// 澶勭悊閫氱煡闃熷垪
    /// </summary>
    private IEnumerator ProcessNotificationQueue()
    {
        isShowingNotification = true;

        while (notificationQueue.Count > 0)
        {
            var notification = notificationQueue.Dequeue();
            yield return StartCoroutine(ShowNotification(notification));
            yield return new WaitForSeconds(0.3f);
        }

        isShowingNotification = false;
    }

    /// <summary>
    /// 鏄剧ず閫氱煡寮圭獥
    /// </summary>
    private IEnumerator ShowNotification(MissionNotification notification)
    {
        GameObject notifObj = new GameObject("Notification");
        notifObj.transform.SetParent(notificationCanvas.transform, false);

        RectTransform rect = notifObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(350, 100);
        rect.anchoredPosition = new Vector2(400, -20);

        Image bg = notifObj.AddComponent<Image>();
        bg.color = notification.color;

        // 鏍囬
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(notifObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 30);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = notification.title;
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = Color.white;
        if (FontManager.Instance != null)
        {
            titleText.font = FontManager.Instance.ChineseFont;
        }

        // 娑堟伅
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(notifObj.transform, false);
        RectTransform msgRect = msgObj.AddComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0, 0);
        msgRect.anchorMax = new Vector2(1, 1);
        msgRect.offsetMin = new Vector2(10, 10);
        msgRect.offsetMax = new Vector2(-10, -45);

        TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
        msgText.text = notification.message;
        msgText.fontSize = 18;
        msgText.alignment = TextAlignmentOptions.Left;
        msgText.color = Color.white;
        if (FontManager.Instance != null)
        {
            msgText.font = FontManager.Instance.ChineseFont;
        }

        // 婊戝叆鍔ㄧ敾
        float elapsed = 0f;
        float slideInDuration = 0.3f;
        Vector2 startPos = new Vector2(400, -20);
        Vector2 endPos = new Vector2(-20, -20);

        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideInDuration;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(notification.duration);

        // 婊戝嚭鍔ㄧ敾
        elapsed = 0f;
        float slideOutDuration = 0.3f;
        startPos = rect.anchoredPosition;
        endPos = new Vector2(400, -20);

        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideOutDuration;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            bg.color = new Color(notification.color.r, notification.color.g, notification.color.b, notification.color.a * (1 - t));
            yield return null;
        }

        Destroy(notifObj);
    }

    /// <summary>
    /// 娣诲姞杩借釜椤?    /// </summary>
    private void AddTrackerItem(MissionDefinition mission)
    {
        if (mission == null || trackerContent == null || trackerPanel == null) return;
        if (trackerItems.ContainsKey(mission.missionId)) return;

        GameObject itemObj = new GameObject($"TrackerItem_{mission.missionId}");
        itemObj.transform.SetParent(trackerContent, false);

        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 120);

        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 浠诲姟鍚嶇О
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(itemObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -5);
        nameRect.sizeDelta = new Vector2(-10, 25);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = mission.missionName;
        nameText.fontSize = 18;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = mission.type == MissionType.MainStory ? new Color(1f, 0.8f, 0.2f) : Color.white;
        if (FontManager.Instance != null)
        {
            nameText.font = FontManager.Instance.ChineseFont;
        }

        // 鐩爣鍒楄〃
        GameObject objectivesObj = new GameObject("Objectives");
        objectivesObj.transform.SetParent(itemObj.transform, false);
        RectTransform objRect = objectivesObj.AddComponent<RectTransform>();
        objRect.anchorMin = new Vector2(0, 0);
        objRect.anchorMax = new Vector2(1, 1);
        objRect.offsetMin = new Vector2(5, 5);
        objRect.offsetMax = new Vector2(-5, -35);

        TextMeshProUGUI objText = objectivesObj.AddComponent<TextMeshProUGUI>();
        objText.fontSize = 14;
        objText.alignment = TextAlignmentOptions.TopLeft;
        objText.color = new Color(0.8f, 0.8f, 0.8f);
        if (FontManager.Instance != null)
        {
            objText.font = FontManager.Instance.ChineseFont;
        }

        trackerItems[mission.missionId] = itemObj;
        UpdateTrackerItem(mission);

        if (!trackerPanel.activeSelf)
        {
            trackerPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 鏇存柊杩借釜椤?    /// </summary>
    private void UpdateTrackerItem(MissionDefinition mission)
    {
        if (mission == null || MissionSystem.Instance == null) return;
        if (!trackerItems.TryGetValue(mission.missionId, out var itemObj)) return;

        var runtimeData = MissionSystem.Instance.GetMissionRuntimeData(mission.missionId);
        if (runtimeData == null) return;

        var objText = itemObj.transform.Find("Objectives").GetComponent<TextMeshProUGUI>();
        string text = "";

        foreach (var objective in runtimeData.objectives)
        {
            string checkmark = objective.isCompleted ? "[OK]" : "[ ]";
            string progress = $"{objective.currentValue}/{objective.targetValue}";
            text += $"{checkmark} {objective.description} ({progress})\n";
        }

        objText.text = text.TrimEnd('\n');
    }

    /// <summary>
    /// 绉婚櫎杩借釜椤?    /// </summary>
    private void RemoveTrackerItem(string missionId)
    {
        if (trackerItems.TryGetValue(missionId, out var itemObj))
        {
            Destroy(itemObj);
            trackerItems.Remove(missionId);
        }

        if (trackerItems.Count == 0)
        {
            trackerPanel.SetActive(false);
        }
    }

    private void RebuildTrackerFromState()
    {
        if (trackerContent == null || trackerPanel == null)
        {
            return;
        }

        foreach (var item in trackerItems.Values)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        trackerItems.Clear();

        if (MissionSystem.Instance == null)
        {
            trackerPanel.SetActive(false);
            return;
        }

        List<MissionDefinition> activeMissions = MissionSystem.Instance.GetActiveMissions();
        if (activeMissions == null || activeMissions.Count == 0)
        {
            trackerPanel.SetActive(false);
            return;
        }

        foreach (MissionDefinition mission in activeMissions)
        {
            AddTrackerItem(mission);
        }

        trackerPanel.SetActive(trackerItems.Count > 0);
    }

    /// <summary>
    /// 鏄剧ず浠诲姟瀹屾垚寮圭獥
    /// </summary>
    private void ShowCompletionPopup(MissionDefinition mission)
    {
        StartCoroutine(ShowCompletionPopupCoroutine(mission));
    }

    private IEnumerator ShowCompletionPopupCoroutine(MissionDefinition mission)
    {
        GameObject popupObj = new GameObject("CompletionPopup");
        popupObj.transform.SetParent(notificationCanvas.transform, false);

        RectTransform rect = popupObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(500, 300);
        rect.localScale = Vector3.zero;

        Image bg = popupObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 鏍囬
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(popupObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(-40, 50);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Mission Complete!";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.8f, 0.2f);
        if (FontManager.Instance != null)
        {
            titleText.font = FontManager.Instance.ChineseFont;
        }

        // 浠诲姟鍚嶇О
        GameObject nameObj = new GameObject("MissionName");
        nameObj.transform.SetParent(popupObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -80);
        nameRect.sizeDelta = new Vector2(-40, 40);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = mission.missionName;
        nameText.fontSize = 24;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        if (FontManager.Instance != null)
        {
            nameText.font = FontManager.Instance.ChineseFont;
        }

        // 濂栧姳鍒楄〃
        GameObject rewardsObj = new GameObject("Rewards");
        rewardsObj.transform.SetParent(popupObj.transform, false);
        RectTransform rewardsRect = rewardsObj.AddComponent<RectTransform>();
        rewardsRect.anchorMin = new Vector2(0, 0);
        rewardsRect.anchorMax = new Vector2(1, 1);
        rewardsRect.offsetMin = new Vector2(20, 60);
        rewardsRect.offsetMax = new Vector2(-20, -130);

        TextMeshProUGUI rewardsText = rewardsObj.AddComponent<TextMeshProUGUI>();
        rewardsText.fontSize = 20;
        rewardsText.alignment = TextAlignmentOptions.Center;
        rewardsText.color = new Color(0.2f, 1f, 0.2f);
        if (FontManager.Instance != null)
        {
            rewardsText.font = FontManager.Instance.ChineseFont;
        }

        string rewardStr = "濂栧姳锛歕n";
        if (mission.rewards != null && mission.rewards.Count > 0)
        {
            foreach (var reward in mission.rewards)
            {
                rewardStr += $"- {reward.description}\n";
            }
        }
        else
        {
            rewardStr += "无";
        }
        rewardsText.text = rewardStr;

        // 鍏抽棴鎸夐挳
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(popupObj.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0);
        btnRect.anchorMax = new Vector2(0.5f, 0);
        btnRect.pivot = new Vector2(0.5f, 0);
        btnRect.anchoredPosition = new Vector2(0, 15);
        btnRect.sizeDelta = new Vector2(150, 40);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.3f, 0.6f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(() => Destroy(popupObj));

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "纭畾";
        btnText.fontSize = 20;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        if (FontManager.Instance != null)
        {
            btnText.font = FontManager.Instance.ChineseFont;
        }

        // 寮瑰嚭鍔ㄧ敾
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            rect.localScale = Vector3.one * t;
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    private struct MissionNotification
    {
        public string title;
        public string message;
        public Color color;
        public float duration;
    }
}
