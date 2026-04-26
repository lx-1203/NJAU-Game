using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 任务面板构建器
/// 显示所有任务（进行中/已完成/可接取）
/// </summary>
public class MissionPanelBuilder : MonoBehaviour
{
    public static MissionPanelBuilder Instance { get; private set; }

    private Canvas canvas;
    private GameObject panel;
    private Transform contentTransform;
    private bool isOpen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CreatePanel();
    }

    private void Update()
    {
        // 按J键打开/关闭任务面板
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        // ESC关闭
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    /// <summary>
    /// 创建任务面板
    /// </summary>
    private void CreatePanel()
    {
        GameObject canvasObj = new GameObject("MissionPanelCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // 半透明背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        Button bgButton = bgObj.AddComponent<Button>();
        bgButton.onClick.AddListener(ClosePanel);

        // 主面板
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900, 700);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // 标题
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 50);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "任务列表";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        if (FontManager.Instance != null)
        {
            titleText.font = FontManager.Instance.ChineseFont;
        }

        // 关闭按钮
        GameObject closeBtn = CreateButton("CloseButton", panel.transform, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-10, -10), new Vector2(50, 50), "×", 36);
        closeBtn.GetComponent<Button>().onClick.AddListener(ClosePanel);
        closeBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

        // ScrollView
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20, 20);
        scrollRect.offsetMax = new Vector2(-20, -70);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        contentTransform = contentObj.transform;
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        canvasObj.SetActive(false);
    }

    /// <summary>
    /// 打开面板
    /// </summary>
    public void OpenPanel()
    {
        if (MissionSystem.Instance == null)
        {
            Debug.LogWarning("[MissionPanelBuilder] MissionSystem not found");
            return;
        }

        canvas.gameObject.SetActive(true);
        isOpen = true;
        RefreshPanel();
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    public void ClosePanel()
    {
        canvas.gameObject.SetActive(false);
        isOpen = false;
    }

    public bool IsOpen => isOpen;

    /// <summary>
    /// 刷新面板内容
    /// </summary>
    private void RefreshPanel()
    {
        // 清空现有内容
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        var activeMissions = MissionSystem.Instance.GetActiveMissions();
        var completedMissions = MissionSystem.Instance.GetCompletedMissions();

        // 进行中的任务
        if (activeMissions.Count > 0)
        {
            CreateSectionHeader("进行中", new Color(0.3f, 0.6f, 1f));
            foreach (var mission in activeMissions.OrderBy(m => m.priority))
            {
                CreateMissionItem(mission, MissionStatus.Active);
            }
        }

        // 已完成的任务
        if (completedMissions.Count > 0)
        {
            CreateSectionHeader("已完成", new Color(0.2f, 0.8f, 0.2f));
            foreach (var mission in completedMissions.OrderBy(m => m.priority))
            {
                CreateMissionItem(mission, MissionStatus.Completed);
            }
        }

        if (activeMissions.Count == 0 && completedMissions.Count == 0)
        {
            CreateEmptyMessage();
        }
    }

    /// <summary>
    /// 创建分组标题
    /// </summary>
    private void CreateSectionHeader(string title, Color color)
    {
        GameObject headerObj = new GameObject($"Header_{title}");
        headerObj.transform.SetParent(contentTransform, false);

        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 40);

        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(color.r, color.g, color.b, 0.3f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(headerObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        if (FontManager.Instance != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }
    }

    /// <summary>
    /// 创建任务项
    /// </summary>
    private void CreateMissionItem(MissionDefinition mission, MissionStatus status)
    {
        GameObject itemObj = new GameObject($"Mission_{mission.missionId}");
        itemObj.transform.SetParent(contentTransform, false);

        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 150);

        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // 任务类型标签
        GameObject typeObj = new GameObject("Type");
        typeObj.transform.SetParent(itemObj.transform, false);
        RectTransform typeRect = typeObj.AddComponent<RectTransform>();
        typeRect.anchorMin = new Vector2(0, 1);
        typeRect.anchorMax = new Vector2(0, 1);
        typeRect.pivot = new Vector2(0, 1);
        typeRect.anchoredPosition = new Vector2(10, -10);
        typeRect.sizeDelta = new Vector2(80, 30);

        Image typeBg = typeObj.AddComponent<Image>();
        typeBg.color = mission.type == MissionType.MainStory ? new Color(1f, 0.5f, 0.2f) : new Color(0.3f, 0.6f, 1f);

        GameObject typeTextObj = new GameObject("Text");
        typeTextObj.transform.SetParent(typeObj.transform, false);
        RectTransform typeTextRect = typeTextObj.AddComponent<RectTransform>();
        typeTextRect.anchorMin = Vector2.zero;
        typeTextRect.anchorMax = Vector2.one;
        typeTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI typeText = typeTextObj.AddComponent<TextMeshProUGUI>();
        typeText.text = mission.type == MissionType.MainStory ? "主线" : "支线";
        typeText.fontSize = 16;
        typeText.alignment = TextAlignmentOptions.Center;
        typeText.color = Color.white;
        if (FontManager.Instance != null)
        {
            typeText.font = FontManager.Instance.ChineseFont;
        }

        // 任务名称
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(itemObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -10);
        nameRect.sizeDelta = new Vector2(-200, 30);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = mission.missionName;
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;
        if (FontManager.Instance != null)
        {
            nameText.font = FontManager.Instance.ChineseFont;
        }

        // 任务描述
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(itemObj.transform, false);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 1);
        descRect.anchorMax = new Vector2(1, 1);
        descRect.pivot = new Vector2(0.5f, 1);
        descRect.anchoredPosition = new Vector2(0, -45);
        descRect.sizeDelta = new Vector2(-20, 40);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = mission.description;
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = new Color(0.7f, 0.7f, 0.7f);
        if (FontManager.Instance != null)
        {
            descText.font = FontManager.Instance.ChineseFont;
        }

        // 目标进度
        if (status == MissionStatus.Active)
        {
            var runtimeData = MissionSystem.Instance.GetMissionRuntimeData(mission.missionId);
            if (runtimeData != null)
            {
                GameObject objObj = new GameObject("Objectives");
                objObj.transform.SetParent(itemObj.transform, false);
                RectTransform objRect = objObj.AddComponent<RectTransform>();
                objRect.anchorMin = new Vector2(0, 0);
                objRect.anchorMax = new Vector2(1, 0);
                objRect.pivot = new Vector2(0.5f, 0);
                objRect.anchoredPosition = new Vector2(0, 10);
                objRect.sizeDelta = new Vector2(-20, 50);

                TextMeshProUGUI objText = objObj.AddComponent<TextMeshProUGUI>();
                objText.fontSize = 14;
                objText.alignment = TextAlignmentOptions.TopLeft;
                objText.color = new Color(0.8f, 0.8f, 0.8f);
                if (FontManager.Instance != null)
                {
                    objText.font = FontManager.Instance.ChineseFont;
                }

                string objStr = "";
                foreach (var objective in runtimeData.objectives)
                {
                    string checkmark = objective.isCompleted ? "✓" : "○";
                    string progress = $"{objective.currentValue}/{objective.targetValue}";
                    objStr += $"{checkmark} {objective.description} ({progress})\n";
                }
                objText.text = objStr.TrimEnd('\n');
            }
        }
        else if (status == MissionStatus.Completed)
        {
            GameObject completeObj = new GameObject("Completed");
            completeObj.transform.SetParent(itemObj.transform, false);
            RectTransform completeRect = completeObj.AddComponent<RectTransform>();
            completeRect.anchorMin = new Vector2(1, 0.5f);
            completeRect.anchorMax = new Vector2(1, 0.5f);
            completeRect.pivot = new Vector2(1, 0.5f);
            completeRect.anchoredPosition = new Vector2(-10, 0);
            completeRect.sizeDelta = new Vector2(100, 40);

            TextMeshProUGUI completeText = completeObj.AddComponent<TextMeshProUGUI>();
            completeText.text = "✓ 已完成";
            completeText.fontSize = 20;
            completeText.fontStyle = FontStyles.Bold;
            completeText.alignment = TextAlignmentOptions.Center;
            completeText.color = new Color(0.2f, 1f, 0.2f);
            if (FontManager.Instance != null)
            {
                completeText.font = FontManager.Instance.ChineseFont;
            }
        }
    }

    /// <summary>
    /// 创建空消息
    /// </summary>
    private void CreateEmptyMessage()
    {
        GameObject msgObj = new GameObject("EmptyMessage");
        msgObj.transform.SetParent(contentTransform, false);

        RectTransform msgRect = msgObj.AddComponent<RectTransform>();
        msgRect.sizeDelta = new Vector2(0, 100);

        TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
        msgText.text = "暂无任务";
        msgText.fontSize = 24;
        msgText.alignment = TextAlignmentOptions.Center;
        msgText.color = new Color(0.5f, 0.5f, 0.5f);
        if (FontManager.Instance != null)
        {
            msgText.font = FontManager.Instance.ChineseFont;
        }
    }

    /// <summary>
    /// 创建按钮
    /// </summary>
    private GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta, string text, int fontSize)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = anchorMin;
        btnRect.anchorMax = anchorMax;
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = anchoredPosition;
        btnRect.sizeDelta = sizeDelta;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.3f, 0.3f, 0.3f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = fontSize;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        if (FontManager.Instance != null)
        {
            btnText.font = FontManager.Instance.ChineseFont;
        }

        return btnObj;
    }
}
