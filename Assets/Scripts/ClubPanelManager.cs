using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 社团面板管理器 —— 负责数据绑定、刷新显示、处理按钮事件
/// 由外部动态挂载到 ClubPanelBuilder 所在的 GameObject 上
/// </summary>
public class ClubPanelManager : MonoBehaviour
{
    // ========== 引用 ==========

    private ClubPanelBuilder builder;
    private string selectedClubId;
    private TextMeshProUGUI detailHintText;  // 动态创建的提示文本
    private Dictionary<string, GameObject> clubListItems = new Dictionary<string, GameObject>();

    // ========== 列表项颜色（与 Builder 保持一致） ==========

    private static readonly Color ItemNormalColor   = new Color(0.12f, 0.12f, 0.18f, 0.85f);
    private static readonly Color ItemSelectedColor = new Color(0.20f, 0.25f, 0.40f, 0.95f);
    private static readonly Color BtnJoinColor      = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnLeaveColor     = new Color(0.55f, 0.20f, 0.20f, 1.0f);
    private static readonly Color TextGray          = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color BtnDisabledColor  = new Color(0.35f, 0.35f, 0.40f, 0.7f);
    private static readonly Color TextWarning        = new Color(0.90f, 0.55f, 0.20f);

    // ========== 初始化 ==========

    /// <summary>
    /// 初始化面板管理器，绑定事件
    /// </summary>
    public void Initialize(ClubPanelBuilder panelBuilder)
    {
        builder = panelBuilder;

        // 绑定关闭按钮
        if (builder.btnClose != null)
            builder.btnClose.onClick.AddListener(ClosePanel);

        // 绑定遮罩点击关闭
        Transform overlay = builder.clubCanvas.transform.Find("Overlay");
        if (overlay != null)
        {
            Button overlayBtn = overlay.GetComponent<Button>();
            if (overlayBtn != null)
                overlayBtn.onClick.AddListener(ClosePanel);
        }

        // 绑定活动按钮
        if (builder.btnActivity != null)
            builder.btnActivity.onClick.AddListener(OnActivityClicked);

        // 绑定加入/退出按钮
        if (builder.btnJoinLeave != null)
            builder.btnJoinLeave.onClick.AddListener(OnJoinLeaveClicked);

        // 绑定入党按钮
        if (builder.btnPartyApply != null)
            builder.btnPartyApply.onClick.AddListener(OnPartyApplyClicked);

        // 订阅 ClubSystem 事件
        if (ClubSystem.Instance != null)
        {
            ClubSystem.Instance.OnClubStateChanged += RefreshAll;
        }
    }

    // ========== 面板开关 ==========

    /// <summary>打开社团面板</summary>
    public void OpenPanel()
    {
        if (builder == null || builder.panelRoot == null) return;

        builder.panelRoot.SetActive(true);

        // 同时显示遮罩
        Transform overlay = builder.clubCanvas.transform.Find("Overlay");
        if (overlay != null)
            overlay.gameObject.SetActive(true);

        RefreshAll();
    }

    /// <summary>关闭社团面板</summary>
    public void ClosePanel()
    {
        if (builder == null || builder.panelRoot == null) return;

        builder.panelRoot.SetActive(false);

        // 同时隐藏遮罩
        Transform overlay = builder.clubCanvas.transform.Find("Overlay");
        if (overlay != null)
            overlay.gameObject.SetActive(false);
    }

    /// <summary>面板是否处于打开状态</summary>
    public bool IsOpen => builder != null && builder.panelRoot != null && builder.panelRoot.activeSelf;

    // ========== 刷新方法 ==========

    /// <summary>刷新所有内容（列表 + 详情 + 入党）</summary>
    public void RefreshAll()
    {
        RefreshClubList();
        RefreshDetail();
        RefreshPartySection();
    }

    /// <summary>刷新左侧社团列表</summary>
    public void RefreshClubList()
    {
        if (builder == null || builder.listContent == null) return;
        if (ClubSystem.Instance == null) return;

        // 清空旧列表项
        for (int i = builder.listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(builder.listContent.GetChild(i).gameObject);
        }
        clubListItems.Clear();

        // 获取所有社团定义
        List<ClubDefinition> allClubs = ClubSystem.Instance.GetAllClubs();

        // --- 分组：已加入 ---
        List<ClubDefinition> joinedClubs = new List<ClubDefinition>();
        List<ClubDefinition> availableClubs = new List<ClubDefinition>();
        List<ClubDefinition> specialClubs = new List<ClubDefinition>();

        foreach (var club in allClubs)
        {
            bool isJoined = ClubSystem.Instance.IsInClub(club.id);

            if (!club.occupiesSlot)
            {
                specialClubs.Add(club);
            }
            else if (isJoined)
            {
                joinedClubs.Add(club);
            }
            else
            {
                availableClubs.Add(club);
            }
        }

        string firstClubId = null;

        // 已加入
        if (joinedClubs.Count > 0 || true) // 始终显示分组标题
        {
            builder.CreateSectionHeader("【已加入】", builder.listContent);
            foreach (var club in joinedClubs)
            {
                CreateAndRegisterItem(club, true);
                if (firstClubId == null) firstClubId = club.id;
            }
        }

        // 可加入
        builder.CreateSectionHeader("【可加入】", builder.listContent);
        foreach (var club in availableClubs)
        {
            CreateAndRegisterItem(club, false);
            if (firstClubId == null) firstClubId = club.id;
        }

        // 特殊组织
        if (specialClubs.Count > 0)
        {
            builder.CreateSectionHeader("【特殊组织】", builder.listContent);
            foreach (var club in specialClubs)
            {
                bool isJoined = ClubSystem.Instance.IsInClub(club.id);
                CreateAndRegisterItem(club, isJoined);
                if (firstClubId == null) firstClubId = club.id;
            }
        }

        // 默认选中
        if (string.IsNullOrEmpty(selectedClubId) || !clubListItems.ContainsKey(selectedClubId))
        {
            selectedClubId = firstClubId;
        }

        // 高亮当前选中
        HighlightSelectedItem();
    }

    /// <summary>刷新右侧详情区（基于 selectedClubId）</summary>
    public void RefreshDetail()
    {
        if (builder == null) return;
        if (ClubSystem.Instance == null) return;

        // 找不到社团或未选中 → 隐藏详情内容
        if (string.IsNullOrEmpty(selectedClubId))
        {
            SetDetailVisible(false);
            return;
        }

        ClubDefinition club = ClubSystem.Instance.GetClub(selectedClubId);
        if (club == null)
        {
            SetDetailVisible(false);
            return;
        }

        SetDetailVisible(true);

        // 确保提示文本存在
        EnsureHintText();

        // 社团名称
        if (builder.detailName != null)
            builder.detailName.text = club.name;

        // 分类 + 主属性
        if (builder.detailInfo != null)
            builder.detailInfo.text = $"分类：{club.category}  主要属性：{club.primaryAttribute}";

        bool isJoined = ClubSystem.Instance.IsInClub(selectedClubId);

        if (isJoined)
        {
            // --- 已加入：显示职务与晋升 ---
            PromotionRank currentRank = ClubSystem.Instance.GetCurrentRank(selectedClubId);

            if (builder.detailPosition != null)
            {
                builder.detailPosition.text = currentRank != null
                    ? $"当前职务：{currentRank.title}"
                    : "当前职务：干事";
                builder.detailPosition.gameObject.SetActive(true);
            }

            // 晋升条件
            PromotionRank nextRank = ClubSystem.Instance.GetNextRank(selectedClubId);
            if (builder.detailNextRank != null)
            {
                if (nextRank != null)
                {
                    string conditionText = $"下次晋升：{nextRank.title}\n  条件：{nextRank.requiredRounds}回合";
                    if (nextRank.requiredAttributes != null && nextRank.requiredAttributes.Length > 0)
                    {
                        foreach (var req in nextRank.requiredAttributes)
                        {
                            conditionText += $" + {req.attributeName}≥{req.minValue}";
                        }
                    }
                    builder.detailNextRank.text = conditionText;
                }
                else
                {
                    builder.detailNextRank.text = "已达到最高职务";
                }
                builder.detailNextRank.gameObject.SetActive(true);
            }

            // 显示活动按钮（检查活动次数限制）
            if (builder.btnActivity != null)
            {
                builder.btnActivity.gameObject.SetActive(true);
                bool canActivity = ClubSystem.Instance.CanDoClubActivity(selectedClubId);
                builder.btnActivity.interactable = canActivity;
                if (!canActivity && ClubSystem.Instance.HasActivityThisRound(selectedClubId))
                {
                    SetHintText("本回合已参加过该社团活动");
                }
            }

            // 按钮 → 退出社团（官方组织不可退出）
            if (builder.btnJoinLeave != null)
            {
                bool canLeave = ClubSystem.Instance.CanLeaveClub(selectedClubId);
                builder.btnJoinLeave.gameObject.SetActive(true);
                builder.btnJoinLeave.interactable = canLeave;
                SetButtonColor(builder.btnJoinLeave, canLeave ? BtnLeaveColor : BtnDisabledColor);
            }
            if (builder.btnJoinLeaveText != null)
            {
                if (club.isOfficial)
                    builder.btnJoinLeaveText.text = "官方组织";
                else
                    builder.btnJoinLeaveText.text = "退出社团";
            }
        }
        else
        {
            // --- 未加入 ---
            if (builder.detailPosition != null)
            {
                builder.detailPosition.text = "未加入";
                builder.detailPosition.gameObject.SetActive(true);
            }

            if (builder.detailNextRank != null)
                builder.detailNextRank.gameObject.SetActive(false);

            // 隐藏活动按钮
            if (builder.btnActivity != null)
                builder.btnActivity.gameObject.SetActive(false);

            // 按钮 → 加入社团（含条件提示）
            if (builder.btnJoinLeave != null)
            {
                bool canJoin = ClubSystem.Instance.CanJoinClub(selectedClubId);
                builder.btnJoinLeave.gameObject.SetActive(true);
                builder.btnJoinLeave.interactable = canJoin;
                SetButtonColor(builder.btnJoinLeave, canJoin ? BtnJoinColor : BtnDisabledColor);
                if (!canJoin)
                {
                    string reason = ClubSystem.Instance.GetJoinBlockReason(selectedClubId);
                    if (!string.IsNullOrEmpty(reason))
                        SetHintText(reason);
                }
            }
            if (builder.btnJoinLeaveText != null)
                builder.btnJoinLeaveText.text = "加入社团";
        }
    }

    /// <summary>刷新入党进度区域</summary>
    public void RefreshPartySection()
    {
        if (builder == null || builder.partySection == null) return;
        if (ClubSystem.Instance == null) return;

        int currentStage = ClubSystem.Instance.CurrentPartyStage;
        int totalStages = ClubSystem.Instance.PartyStageCount;

        // 进度条
        if (builder.partyProgressFill != null)
        {
            float progress = totalStages > 1 ? (float)currentStage / (totalStages - 1) : 0f;
            builder.partyProgressFill.fillAmount = progress;
        }

        // 阶段文字
        if (builder.partyStageText != null)
        {
            string stageName = ClubSystem.Instance.CurrentPartyStageName;
            builder.partyStageText.text = totalStages > 1
                ? $"{stageName} ({currentStage}/{totalStages - 1})"
                : stageName;
        }

        // 按钮状态
        if (builder.btnPartyApply != null && builder.btnPartyApplyText != null)
        {
            if (currentStage == 0)
            {
                builder.btnPartyApplyText.text = "申请入党";
                builder.btnPartyApply.interactable = ClubSystem.Instance.CanApplyForParty();
                if (!builder.btnPartyApply.interactable)
                {
                    string reason = ClubSystem.Instance.GetPartyBlockReason();
                    if (!string.IsNullOrEmpty(reason))
                    {
                        builder.btnPartyApplyText.text = reason;
                    }
                }
            }
            else if (currentStage >= totalStages - 1)
            {
                builder.btnPartyApplyText.text = "已成为正式党员";
                builder.btnPartyApply.interactable = false;
            }
            else
            {
                builder.btnPartyApplyText.text = "进行中...";
                builder.btnPartyApply.interactable = false;
            }
        }
    }

    // ========== 按钮回调 ==========

    private void OnActivityClicked()
    {
        if (string.IsNullOrEmpty(selectedClubId)) return;
        if (ClubSystem.Instance == null) return;

        ClubSystem.Instance.DoClubActivity(selectedClubId);
    }

    private void OnJoinLeaveClicked()
    {
        if (string.IsNullOrEmpty(selectedClubId)) return;
        if (ClubSystem.Instance == null) return;

        if (ClubSystem.Instance.IsInClub(selectedClubId))
        {
            if (!ClubSystem.Instance.CanLeaveClub(selectedClubId))
            {
                Debug.Log("[ClubPanelManager] 该社团不可退出");
                return;
            }
            ClubSystem.Instance.LeaveClub(selectedClubId);
        }
        else
        {
            ClubSystem.Instance.JoinClub(selectedClubId);
        }
    }

    private void OnPartyApplyClicked()
    {
        if (ClubSystem.Instance == null) return;

        ClubSystem.Instance.ApplyForParty();
    }

    /// <summary>选中一个社团</summary>
    private void SelectClub(string clubId)
    {
        selectedClubId = clubId;
        HighlightSelectedItem();
        RefreshDetail();
    }

    // ========== 辅助方法 ==========

    /// <summary>创建列表项并注册到字典</summary>
    private void CreateAndRegisterItem(ClubDefinition club, bool isJoined)
    {
        GameObject item = builder.CreateClubListItem(club.name, isJoined, builder.listContent);
        clubListItems[club.id] = item;

        // 绑定点击事件（需要局部变量捕获）
        string capturedId = club.id;
        Button btn = item.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => SelectClub(capturedId));
        }
    }

    /// <summary>高亮选中的列表项</summary>
    private void HighlightSelectedItem()
    {
        foreach (var kvp in clubListItems)
        {
            Image bg = kvp.Value.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = (kvp.Key == selectedClubId) ? ItemSelectedColor : ItemNormalColor;
            }

            // 同步 Button 的 normalColor 以避免 hover 后复位到错误颜色
            Button btn = kvp.Value.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = (kvp.Key == selectedClubId) ? ItemSelectedColor : ItemNormalColor;
                cb.selectedColor = (kvp.Key == selectedClubId) ? ItemSelectedColor : ItemNormalColor;
                btn.colors = cb;
            }
        }
    }

    /// <summary>设置详情区内容的可见性</summary>
    private void SetDetailVisible(bool visible)
    {
        if (builder.detailName != null)
            builder.detailName.gameObject.SetActive(visible);
        if (builder.detailInfo != null)
            builder.detailInfo.gameObject.SetActive(visible);
        if (builder.detailPosition != null)
            builder.detailPosition.gameObject.SetActive(visible);
        if (builder.detailNextRank != null)
            builder.detailNextRank.gameObject.SetActive(visible);
        if (builder.btnActivity != null)
            builder.btnActivity.gameObject.SetActive(visible);
        if (builder.btnJoinLeave != null)
            builder.btnJoinLeave.gameObject.SetActive(visible);
    }

    /// <summary>确保提示文本 UI 存在</summary>
    private void EnsureHintText()
    {
        if (detailHintText != null)
        {
            detailHintText.text = "";
            return;
        }

        if (builder == null || builder.detailContainer == null) return;

        GameObject hintGo = new GameObject("HintText", typeof(RectTransform));
        hintGo.transform.SetParent(builder.detailContainer, false);

        detailHintText = hintGo.AddComponent<TextMeshProUGUI>();
        detailHintText.fontSize = 13;
        detailHintText.color = TextWarning;
        detailHintText.alignment = TextAlignmentOptions.Left;
        detailHintText.enableWordWrapping = true;
        detailHintText.text = "";

        RectTransform rt = hintGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 40);
    }

    /// <summary>设置提示文本内容</summary>
    private void SetHintText(string text)
    {
        if (detailHintText != null)
            detailHintText.text = text;
    }

    /// <summary>修改按钮的背景色和颜色过渡</summary>
    private void SetButtonColor(Button btn, Color normalColor)
    {
        Image bg = btn.GetComponent<Image>();
        if (bg != null)
            bg.color = normalColor;

        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = new Color(
            Mathf.Min(normalColor.r + 0.10f, 1f),
            Mathf.Min(normalColor.g + 0.10f, 1f),
            Mathf.Min(normalColor.b + 0.10f, 1f),
            normalColor.a
        );
        cb.pressedColor = new Color(
            Mathf.Max(normalColor.r - 0.05f, 0f),
            Mathf.Max(normalColor.g - 0.05f, 0f),
            Mathf.Max(normalColor.b - 0.05f, 0f),
            normalColor.a
        );
        cb.selectedColor = normalColor;
        btn.colors = cb;
    }

    // ========== 生命周期 ==========

    private void OnDestroy()
    {
        // 取消订阅 ClubSystem 事件
        if (ClubSystem.Instance != null)
        {
            ClubSystem.Instance.OnClubStateChanged -= RefreshAll;
        }
    }
}
