using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 天赋树UI —— 纯代码构建，独立Canvas
/// 4大分支横向排列，每分支4层纵向排列
/// </summary>
public class TalentUI : MonoBehaviour
{
    // ========== 单例 ==========
    public static TalentUI Instance { get; private set; }

    // ========== UI引用 ==========
    private GameObject talentCanvas;
    private TextMeshProUGUI pointsText;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailDesc;
    private TextMeshProUGUI detailCost;
    private Button activateBtn;
    private string selectedTalentId;

    // ========== 颜色定义 ==========
    private static readonly Color AcademicColor = new Color(0.3f, 0.5f, 0.9f);   // 蓝
    private static readonly Color SocialColor = new Color(0.9f, 0.5f, 0.7f);      // 粉
    private static readonly Color PhysicalColor = new Color(0.9f, 0.3f, 0.3f);    // 红
    private static readonly Color MindsetColor = new Color(0.3f, 0.8f, 0.5f);     // 绿
    private static readonly Color InactiveColor = new Color(0.4f, 0.4f, 0.4f);    // 灰
    private static readonly Color CanActivateColor = new Color(1f, 0.9f, 0.5f);   // 金

    private Dictionary<string, Image> talentNodeImages = new Dictionary<string, Image>();

    // ========== 生命周期 ==========

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

    // ========== 公共接口 ==========

    /// <summary>打开天赋树面板</summary>
    public void ShowPanel()
    {
        if (talentCanvas != null) return; // 已打开
        BuildUI();
        RefreshAll();
    }

    /// <summary>关闭天赋树面板</summary>
    public void ClosePanel()
    {
        if (talentCanvas != null)
        {
            Destroy(talentCanvas);
            talentCanvas = null;
        }
        talentNodeImages.Clear();
        selectedTalentId = null;
    }

    public bool IsOpen => talentCanvas != null;

    // ========== UI 构建 ==========

    private void BuildUI()
    {
        talentCanvas = new GameObject("TalentCanvas");
        Canvas canvas = talentCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        CanvasScaler scaler = talentCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        talentCanvas.AddComponent<GraphicRaycaster>();

        // 半透明背景遮罩
        GameObject overlay = CreateElement("Overlay", talentCanvas.transform);
        SetAnchors(overlay, Vector2.zero, Vector2.one);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f);
        Button overlayBtn = overlay.AddComponent<Button>();
        overlayBtn.onClick.AddListener(ClosePanel);

        // 主面板
        GameObject panel = CreateElement("Panel", overlay.transform);
        SetAnchors(panel, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // 标题栏
        GameObject titleBar = CreateElement("TitleBar", panel.transform);
        SetAnchors(titleBar, new Vector2(0, 0.92f), new Vector2(1, 1));
        Image titleBg = titleBar.AddComponent<Image>();
        titleBg.color = new Color(0.15f, 0.15f, 0.25f);

        TextMeshProUGUI titleText = CreateTMP(titleBar.transform, "Title", "天赋树");
        titleText.fontSize = 30;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(titleText.gameObject, new Vector2(0.02f, 0), new Vector2(0.3f, 1));

        // 天赋点显示
        pointsText = CreateTMP(titleBar.transform, "Points", "剩余天赋点: 0");
        pointsText.fontSize = 22;
        pointsText.color = new Color(1f, 0.85f, 0.3f);
        pointsText.alignment = TextAlignmentOptions.MidlineRight;
        SetAnchors(pointsText.gameObject, new Vector2(0.5f, 0), new Vector2(0.85f, 1));

        // 关闭按钮
        GameObject closeBtn = CreateElement("CloseBtn", titleBar.transform);
        SetAnchors(closeBtn, new Vector2(0.92f, 0.15f), new Vector2(0.98f, 0.85f));
        Image closeBg = closeBtn.AddComponent<Image>();
        closeBg.color = new Color(0.8f, 0.2f, 0.2f);
        Button close = closeBtn.AddComponent<Button>();
        close.onClick.AddListener(ClosePanel);
        TextMeshProUGUI closeText = CreateTMP(closeBtn.transform, "X", "X");
        closeText.fontSize = 22;
        closeText.alignment = TextAlignmentOptions.Center;
        SetAnchors(closeText.gameObject, Vector2.zero, Vector2.one);

        // 天赋树区域
        GameObject treeArea = CreateElement("TreeArea", panel.transform);
        SetAnchors(treeArea, new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.91f));

        // 4个分支
        BuildBranch(treeArea.transform, TalentBranch.Academic, 0, "学业", AcademicColor);
        BuildBranch(treeArea.transform, TalentBranch.Social, 1, "社交", SocialColor);
        BuildBranch(treeArea.transform, TalentBranch.Physical, 2, "体魄", PhysicalColor);
        BuildBranch(treeArea.transform, TalentBranch.Mindset, 3, "心境", MindsetColor);

        // 详情面板（底部）
        GameObject detailArea = CreateElement("DetailArea", panel.transform);
        SetAnchors(detailArea, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.17f));
        Image detailBg = detailArea.AddComponent<Image>();
        detailBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        detailName = CreateTMP(detailArea.transform, "DetailName", "选择一个天赋查看详情");
        detailName.fontSize = 20;
        detailName.fontStyle = FontStyles.Bold;
        detailName.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(detailName.gameObject, new Vector2(0.02f, 0.55f), new Vector2(0.4f, 0.95f));

        detailCost = CreateTMP(detailArea.transform, "DetailCost", "");
        detailCost.fontSize = 18;
        detailCost.color = new Color(1f, 0.85f, 0.3f);
        detailCost.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(detailCost.gameObject, new Vector2(0.42f, 0.55f), new Vector2(0.6f, 0.95f));

        detailDesc = CreateTMP(detailArea.transform, "DetailDesc", "");
        detailDesc.fontSize = 16;
        detailDesc.color = new Color(0.8f, 0.8f, 0.8f);
        detailDesc.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(detailDesc.gameObject, new Vector2(0.02f, 0.05f), new Vector2(0.7f, 0.55f));

        // 激活按钮
        GameObject activateBtnObj = CreateElement("ActivateBtn", detailArea.transform);
        SetAnchors(activateBtnObj, new Vector2(0.75f, 0.15f), new Vector2(0.95f, 0.85f));
        Image actBg = activateBtnObj.AddComponent<Image>();
        actBg.color = new Color(0.2f, 0.6f, 0.3f);
        activateBtn = activateBtnObj.AddComponent<Button>();
        activateBtn.onClick.AddListener(OnActivateClicked);
        activateBtn.interactable = false;
        TextMeshProUGUI actText = CreateTMP(activateBtnObj.transform, "ActText", "激活");
        actText.fontSize = 20;
        actText.fontStyle = FontStyles.Bold;
        actText.alignment = TextAlignmentOptions.Center;
        SetAnchors(actText.gameObject, Vector2.zero, Vector2.one);

        // 重置按钮
        GameObject resetBtnObj = CreateElement("ResetBtn", detailArea.transform);
        SetAnchors(resetBtnObj, new Vector2(0.62f, 0.15f), new Vector2(0.73f, 0.85f));
        Image resetBg = resetBtnObj.AddComponent<Image>();
        resetBg.color = new Color(0.6f, 0.3f, 0.2f);
        Button resetBtn = resetBtnObj.AddComponent<Button>();
        resetBtn.onClick.AddListener(OnResetClicked);
        TextMeshProUGUI resetText = CreateTMP(resetBtnObj.transform, "ResetText", "重置(¥500)");
        resetText.fontSize = 14;
        resetText.alignment = TextAlignmentOptions.Center;
        SetAnchors(resetText.gameObject, Vector2.zero, Vector2.one);
    }

    private void BuildBranch(Transform parent, TalentBranch branch, int branchIndex, string branchName, Color branchColor)
    {
        float xMin = branchIndex * 0.25f;
        float xMax = xMin + 0.24f;

        GameObject branchPanel = CreateElement($"Branch_{branch}", parent);
        SetAnchors(branchPanel, new Vector2(xMin, 0), new Vector2(xMax, 1));

        // 分支标题
        TextMeshProUGUI branchTitle = CreateTMP(branchPanel.transform, "Title", branchName);
        branchTitle.fontSize = 22;
        branchTitle.fontStyle = FontStyles.Bold;
        branchTitle.color = branchColor;
        branchTitle.alignment = TextAlignmentOptions.Center;
        SetAnchors(branchTitle.gameObject, new Vector2(0, 0.92f), new Vector2(1, 1));

        if (TalentSystem.Instance == null) return;

        List<TalentDefinition> talents = TalentSystem.Instance.GetTalentsByBranch(branch);

        // 按层分组
        for (int layer = 1; layer <= 4; layer++)
        {
            var layerTalents = talents.FindAll(t => t.layer == layer);
            int count = layerTalents.Count;
            float yMax = 0.90f - (layer - 1) * 0.22f;
            float yMin = yMax - 0.18f;

            for (int i = 0; i < count; i++)
            {
                float nodeXMin = (float)i / count + 0.02f;
                float nodeXMax = (float)(i + 1) / count - 0.02f;

                TalentDefinition talent = layerTalents[i];

                GameObject node = CreateElement($"Node_{talent.id}", branchPanel.transform);
                SetAnchors(node, new Vector2(nodeXMin, yMin), new Vector2(nodeXMax, yMax));

                Image nodeImg = node.AddComponent<Image>();
                nodeImg.color = InactiveColor;
                talentNodeImages[talent.id] = nodeImg;

                Button nodeBtn = node.AddComponent<Button>();
                string capturedId = talent.id;
                nodeBtn.onClick.AddListener(() => OnTalentNodeClicked(capturedId));

                // 名称标签
                TextMeshProUGUI nameLabel = CreateTMP(node.transform, "Name", talent.name);
                nameLabel.fontSize = 12;
                nameLabel.alignment = TextAlignmentOptions.Center;
                nameLabel.enableWordWrapping = true;
                SetAnchors(nameLabel.gameObject, new Vector2(0, 0.1f), new Vector2(1, 0.9f));

                // 消耗标签
                TextMeshProUGUI costLabel = CreateTMP(node.transform, "Cost", $"{talent.cost}TP");
                costLabel.fontSize = 10;
                costLabel.color = new Color(1f, 0.85f, 0.3f);
                costLabel.alignment = TextAlignmentOptions.Center;
                SetAnchors(costLabel.gameObject, new Vector2(0, 0), new Vector2(1, 0.2f));
            }
        }
    }

    // ========== 刷新 ==========

    private void RefreshAll()
    {
        if (TalentSystem.Instance == null) return;

        // 刷新天赋点显示
        if (pointsText != null)
            pointsText.text = $"剩余天赋点: {TalentSystem.Instance.AvailablePoints}";

        // 刷新所有节点颜色
        foreach (var kvp in talentNodeImages)
        {
            string talentId = kvp.Key;
            Image nodeImg = kvp.Value;

            TalentDefinition talent = TalentSystem.Instance.GetTalent(talentId);
            if (talent == null) continue;

            if (TalentSystem.Instance.IsTalentActivated(talentId))
            {
                // 已激活：分支颜色
                nodeImg.color = GetBranchColor(talent.branch);
            }
            else if (TalentSystem.Instance.CanActivateTalent(talentId))
            {
                // 可激活：金色高亮
                nodeImg.color = CanActivateColor;
            }
            else
            {
                // 不可激活：灰色
                nodeImg.color = InactiveColor;
            }
        }

        // 刷新详情面板
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (string.IsNullOrEmpty(selectedTalentId) || TalentSystem.Instance == null)
        {
            if (detailName != null) detailName.text = "选择一个天赋查看详情";
            if (detailDesc != null) detailDesc.text = "";
            if (detailCost != null) detailCost.text = "";
            if (activateBtn != null) activateBtn.interactable = false;
            return;
        }

        TalentDefinition talent = TalentSystem.Instance.GetTalent(selectedTalentId);
        if (talent == null) return;

        bool isActive = TalentSystem.Instance.IsTalentActivated(selectedTalentId);
        bool canActivate = TalentSystem.Instance.CanActivateTalent(selectedTalentId);

        if (detailName != null)
        {
            detailName.text = talent.name;
            detailName.color = isActive ? GetBranchColor(talent.branch) : Color.white;
        }

        if (detailDesc != null)
            detailDesc.text = talent.description;

        if (detailCost != null)
        {
            if (isActive)
                detailCost.text = "已激活";
            else
                detailCost.text = $"消耗: {talent.cost} TP";
        }

        if (activateBtn != null)
            activateBtn.interactable = canActivate;
    }

    private Color GetBranchColor(TalentBranch branch)
    {
        return branch switch
        {
            TalentBranch.Academic => AcademicColor,
            TalentBranch.Social => SocialColor,
            TalentBranch.Physical => PhysicalColor,
            TalentBranch.Mindset => MindsetColor,
            _ => Color.white
        };
    }

    // ========== 交互 ==========

    private void OnTalentNodeClicked(string talentId)
    {
        selectedTalentId = talentId;
        RefreshDetail();
    }

    private void OnActivateClicked()
    {
        if (string.IsNullOrEmpty(selectedTalentId)) return;
        if (TalentSystem.Instance == null) return;

        TalentSystem.Instance.ActivateTalent(selectedTalentId);
        RefreshAll();
    }

    private void OnResetClicked()
    {
        if (TalentSystem.Instance == null) return;
        TalentSystem.Instance.ResetTalents();
        RefreshAll();
    }

    // ========== UI 辅助 ==========

    private GameObject CreateElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void SetAnchors(GameObject obj, Vector2 min, Vector2 max)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return tmp;
    }
}
