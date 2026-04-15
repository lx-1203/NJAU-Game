using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 对话系统 - 单例模式
/// 数据驱动的对话引擎，支持 JSON 对话树、分支选项、条件判断、属性效果
/// 管理对话状态机、逐字显示、选项交互、事件分发
/// 实现 IDialogueTrigger 接口供事件系统调用
/// 已从旧版线性对话系统完全重构
/// </summary>
public class DialogueSystem : MonoBehaviour, IDialogueTrigger
{
    // ========== 单例 ==========
    public static DialogueSystem Instance { get; private set; }

    // ========== 对话状态枚举 ==========
    private enum DialogueState
    {
        Idle,           // 空闲（无对话）
        ShowingText,    // 逐字显示文字中
        WaitingForInput,// 文字显示完毕，等待玩家操作
        ShowingChoices  // 显示选项面板，等待玩家选择
    }

    // ========== 设置 ==========
    [Header("对话设置")]
    [SerializeField] private float textSpeed = 0.04f; // 每个字的显示间隔

    // ========== 事件 ==========
    /// <summary>对话开始时触发，参数为 dialogueId（旧模式为 speakerName）</summary>
    public event Action<string> OnDialogueStart;
    /// <summary>对话结束时触发，参数为 dialogueId（旧模式为 speakerName）</summary>
    public event Action<string> OnDialogueEnd;
    /// <summary>对话结束时触发（无参数版本，供恋爱系统等外部系统订阅）</summary>
    public event Action OnDialogueEnded;
    /// <summary>玩家选择选项时触发</summary>
    public event Action<DialogueChoice> OnChoiceMade;

    // ========== 状态 ==========
    private DialogueState currentState = DialogueState.Idle;
    public bool IsDialogueActive => currentState != DialogueState.Idle;

    // ========== IDialogueTrigger 接口 ==========
    bool IDialogueTrigger.IsActive => IsDialogueActive;

    /// <summary>事件系统通过此回调获知对话结束</summary>
    private Action eventDialogueCompleteCallback;

    /// <summary>事件系统通过此回调获知选项选择</summary>
    private Action<int> eventChoiceCallback;

    /// <summary>事件系统传入的选项数据</summary>
    private EventChoice[] eventChoices;

    // ========== UI 引用 ==========
    private DialogueUIBuilder uiBuilder;

    // ========== 对话数据 ==========
    private DialogueData currentDialogueData;
    private Dictionary<string, DialogueNode> nodeMap;
    private DialogueNode currentNode;
    private string currentDialogueId;

    // ========== 旧模式兼容 ==========
    private string[] legacyLines;
    private int legacyLineIndex;
    private bool isLegacyMode = false;

    // ========== 逐字显示 ==========
    private string currentFullText;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    // ====================================================================
    //  生命周期
    // ====================================================================

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 创建 UI 构建器并构建界面
        uiBuilder = gameObject.AddComponent<DialogueUIBuilder>();
        uiBuilder.BuildDialogueUI();
    }

    private void Start()
    {
        // 订阅 NPCEventHub 对话请求事件（Start 保证所有 Awake 已完成）
        if (NPCEventHub.Instance != null)
        {
            NPCEventHub.Instance.OnDialogueRequested += HandleDialogueRequested;
        }
    }

    private void OnDestroy()
    {
        if (NPCEventHub.Instance != null)
        {
            NPCEventHub.Instance.OnDialogueRequested -= HandleDialogueRequested;
        }
    }

    private void Update()
    {
        if (currentState == DialogueState.Idle) return;

        // 按空格键或鼠标左键推进对话
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }
    }

    // ====================================================================
    //  输入处理
    // ====================================================================

    private void HandleInput()
    {
        switch (currentState)
        {
            case DialogueState.ShowingText:
                // 正在打字 → 立即显示完整文字
                SkipTyping();
                break;

            case DialogueState.WaitingForInput:
                if (isLegacyMode)
                {
                    // 旧模式：推进到下一行
                    LegacyNextLine();
                }
                else
                {
                    // 新模式：检查当前节点
                    if (currentNode.choices != null && currentNode.choices.Length > 0)
                    {
                        // 有选项 → 显示选项面板（不应走到这里，ShowingChoices 状态应已激活）
                        ShowChoices();
                    }
                    else if (!string.IsNullOrEmpty(currentNode.next))
                    {
                        // 有下一节点 → 跳转
                        NavigateToNode(currentNode.next);
                    }
                    else
                    {
                        // 无选项无后续 → 结束对话
                        EndDialogue();
                    }
                }
                break;

            case DialogueState.ShowingChoices:
                // 选项模式下，空格/点击不做任何事（等待玩家点击按钮）
                break;
        }
    }

    // ====================================================================
    //  新接口：通过 JSON 对话 ID 启动
    // ====================================================================

    /// <summary>
    /// 通过对话 ID 启动 JSON 数据驱动对话
    /// </summary>
    /// <param name="dialogueId">JSON 中定义的对话 ID</param>
    public void StartDialogue(string dialogueId)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning("[DialogueSystem] 对话进行中，无法启动新对话");
            return;
        }

        DialogueData data = DialogueParser.GetDialogue(dialogueId);
        if (data == null)
        {
            Debug.LogWarning($"[DialogueSystem] 未找到对话数据: {dialogueId}，尝试旧模式回退");
            return;
        }

        if (data.nodes == null || data.nodes.Length == 0)
        {
            Debug.LogWarning($"[DialogueSystem] 对话 {dialogueId} 没有节点");
            return;
        }

        // 初始化
        isLegacyMode = false;
        currentDialogueData = data;
        currentDialogueId = dialogueId;

        // 构建节点字典
        nodeMap = new Dictionary<string, DialogueNode>();
        foreach (var node in data.nodes)
        {
            if (!string.IsNullOrEmpty(node.id))
            {
                nodeMap[node.id] = node;
            }
        }

        // 显示 UI
        uiBuilder.dialoguePanel.SetActive(true);

        // 触发事件
        OnDialogueStart?.Invoke(dialogueId);

        // 从第一个节点开始
        NavigateToNode(data.nodes[0].id);
    }

    /// <summary>
    /// 兼容旧接口：通过说话人名字和对话行数组启动对话（保留供未迁移的 NPC 使用）
    /// </summary>
    public void StartDialogue(string speakerName, string[] lines)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning("[DialogueSystem] 对话进行中，无法启动新对话");
            return;
        }

        if (lines == null || lines.Length == 0) return;

        // 初始化旧模式
        isLegacyMode = true;
        legacyLines = lines;
        legacyLineIndex = 0;
        currentDialogueId = speakerName;

        // 设置 UI
        uiBuilder.dialoguePanel.SetActive(true);
        uiBuilder.choicePanel.SetActive(false);

        // 名字
        uiBuilder.nameText.text = speakerName;
        uiBuilder.nameContainer.SetActive(true);
        uiBuilder.contentText.fontStyle = FontStyles.Normal;
        uiBuilder.contentText.color = Color.white;

        // 重置内容区域锚点（非旁白模式）
        ResetContentAnchors(false);

        // 头像
        uiBuilder.portraitContainer.SetActive(true);
        Sprite npcSprite = Resources.Load<Sprite>("NPCSprite");
        if (npcSprite != null)
        {
            uiBuilder.portraitImage.sprite = npcSprite;
            uiBuilder.portraitImage.color = Color.white;
        }
        else
        {
            uiBuilder.portraitImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        }

        // 触发事件
        OnDialogueStart?.Invoke(speakerName);

        // 显示第一行
        ShowLine(legacyLines[0]);
    }

    // ====================================================================
    //  节点导航
    // ====================================================================

    /// <summary>
    /// 跳转到指定 ID 的对话节点
    /// </summary>
    private void NavigateToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            EndDialogue();
            return;
        }

        if (!nodeMap.TryGetValue(nodeId, out DialogueNode node))
        {
            Debug.LogError($"[DialogueSystem] 找不到节点: {nodeId}");
            EndDialogue();
            return;
        }

        currentNode = node;

        // 隐藏选项面板
        uiBuilder.choicePanel.SetActive(false);

        // 配置 UI 模式
        ConfigureNodeUI(node);

        // 显示文字
        ShowLine(node.content);
    }

    /// <summary>
    /// 根据节点数据配置 UI 模式（普通对话 / 旁白 / 内心独白）
    /// </summary>
    private void ConfigureNodeUI(DialogueNode node)
    {
        bool isNarrator = string.IsNullOrEmpty(node.speaker);
        bool isInner = node.speaker == "_inner";

        if (isNarrator)
        {
            // ===== 旁白模式 =====
            uiBuilder.portraitContainer.SetActive(false);
            uiBuilder.nameContainer.SetActive(false);
            uiBuilder.contentText.fontStyle = FontStyles.Normal;
            uiBuilder.contentText.color = new Color(0.85f, 0.85f, 0.75f); // 偏暖白
            ResetContentAnchors(true);
        }
        else if (isInner)
        {
            // ===== 内心独白模式 =====
            uiBuilder.portraitContainer.SetActive(false);
            uiBuilder.nameContainer.SetActive(false);
            uiBuilder.contentText.fontStyle = FontStyles.Italic;
            uiBuilder.contentText.color = new Color(0.75f, 0.75f, 0.85f); // 偏紫灰
            ResetContentAnchors(true);
        }
        else
        {
            // ===== 普通对话模式 =====
            uiBuilder.portraitContainer.SetActive(true);
            uiBuilder.nameContainer.SetActive(true);
            uiBuilder.nameText.text = node.speaker;
            uiBuilder.contentText.fontStyle = FontStyles.Normal;
            uiBuilder.contentText.color = Color.white;
            ResetContentAnchors(false);

            // 加载头像
            if (!string.IsNullOrEmpty(node.portrait))
            {
                Sprite portrait = Resources.Load<Sprite>(node.portrait);
                if (portrait != null)
                {
                    uiBuilder.portraitImage.sprite = portrait;
                    uiBuilder.portraitImage.color = Color.white;
                }
                else
                {
                    uiBuilder.portraitImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
                }
            }
            else
            {
                uiBuilder.portraitImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            }
        }
    }

    /// <summary>
    /// 调整内容文字区域的左边距（旁白模式时扩展到左侧）
    /// </summary>
    private void ResetContentAnchors(bool isWideMode)
    {
        RectTransform rt = uiBuilder.contentText.GetComponent<RectTransform>();
        if (isWideMode)
        {
            rt.anchorMin = new Vector2(0.05f, 0.08f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.12f, 0.08f);
        }
    }

    // ====================================================================
    //  逐字显示
    // ====================================================================

    private void ShowLine(string line)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        currentFullText = line;
        typingCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string text)
    {
        currentState = DialogueState.ShowingText;
        isTyping = true;
        uiBuilder.contentText.text = "";
        uiBuilder.hintText.text = "▼";

        foreach (char c in text)
        {
            uiBuilder.contentText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        OnTextComplete();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        uiBuilder.contentText.text = currentFullText;
        isTyping = false;
        OnTextComplete();
    }

    /// <summary>
    /// 文字显示完毕后的处理
    /// </summary>
    private void OnTextComplete()
    {
        if (isLegacyMode)
        {
            // 旧模式：更新提示文字
            if (legacyLineIndex < legacyLines.Length - 1)
            {
                uiBuilder.hintText.text = "按 空格键 继续...";
            }
            else
            {
                uiBuilder.hintText.text = "按 空格键 结束";
            }
            currentState = DialogueState.WaitingForInput;
        }
        else
        {
            // 新模式：检查是否有选项
            if (currentNode.choices != null && currentNode.choices.Length > 0)
            {
                uiBuilder.hintText.text = "";
                ShowChoices();
            }
            else if (!string.IsNullOrEmpty(currentNode.next))
            {
                uiBuilder.hintText.text = "按 空格键 继续...";
                currentState = DialogueState.WaitingForInput;
            }
            else
            {
                uiBuilder.hintText.text = "按 空格键 结束";
                currentState = DialogueState.WaitingForInput;
            }
        }
    }

    // ====================================================================
    //  选项系统
    // ====================================================================

    /// <summary>
    /// 显示当前节点的选项面板
    /// </summary>
    private void ShowChoices()
    {
        currentState = DialogueState.ShowingChoices;

        DialogueChoice[] choices = currentNode.choices;
        int choiceCount = Mathf.Min(choices.Length, 4);

        // 显示选项面板
        uiBuilder.choicePanel.SetActive(true);

        for (int i = 0; i < 4; i++)
        {
            if (i < choiceCount)
            {
                // 显示此选项
                uiBuilder.choiceButtons[i].gameObject.SetActive(true);
                uiBuilder.choiceTexts[i].text = choices[i].text;

                // 条件判断
                bool conditionMet = DialogueParser.EvaluateCondition(choices[i].condition);

                if (conditionMet)
                {
                    // 条件满足：可点击
                    uiBuilder.choiceButtons[i].interactable = true;
                    uiBuilder.choiceTexts[i].color = Color.white;
                    uiBuilder.choiceHints[i].gameObject.SetActive(false);
                }
                else
                {
                    // 条件不满足：灰显
                    uiBuilder.choiceButtons[i].interactable = false;
                    uiBuilder.choiceTexts[i].color = new Color(0.5f, 0.5f, 0.5f);

                    // 显示条件提示
                    if (!string.IsNullOrEmpty(choices[i].conditionHint))
                    {
                        uiBuilder.choiceHints[i].text = choices[i].conditionHint;
                        uiBuilder.choiceHints[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        uiBuilder.choiceHints[i].gameObject.SetActive(false);
                    }
                }

                // 绑定点击事件（先清除旧事件）
                int capturedIndex = i; // 避免闭包问题
                uiBuilder.choiceButtons[i].onClick.RemoveAllListeners();
                uiBuilder.choiceButtons[i].onClick.AddListener(() =>
                {
                    OnChoiceSelected(capturedIndex);
                });
            }
            else
            {
                // 隐藏多余的按钮
                uiBuilder.choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 玩家选择了某个选项
    /// </summary>
    private void OnChoiceSelected(int index)
    {
        if (currentNode.choices == null || index >= currentNode.choices.Length) return;

        DialogueChoice choice = currentNode.choices[index];

        // 应用效果
        if (choice.effects != null && choice.effects.Length > 0)
        {
            ApplyEffects(choice.effects);
        }

        // 触发事件
        OnChoiceMade?.Invoke(choice);

        // 隐藏选项面板
        uiBuilder.choicePanel.SetActive(false);

        // 跳转到目标节点
        if (!string.IsNullOrEmpty(choice.next))
        {
            NavigateToNode(choice.next);
        }
        else
        {
            EndDialogue();
        }
    }

    // ====================================================================
    //  效果应用
    // ====================================================================

    /// <summary>
    /// 应用对话效果（属性变化 / 金钱变化）
    /// 复用项目既有的 PlayerAttributes.AddAttribute() 和 GameState.AddMoney()
    /// </summary>
    private void ApplyEffects(DialogueEffect[] effects)
    {
        if (effects == null) return;

        foreach (var effect in effects)
        {
            switch (effect.type)
            {
                case "attribute":
                    if (PlayerAttributes.Instance != null)
                    {
                        PlayerAttributes.Instance.AddAttribute(effect.target, effect.amount);
                        Debug.Log($"[DialogueSystem] 属性变化: {effect.target} {(effect.amount >= 0 ? "+" : "")}{effect.amount}");
                    }
                    break;

                case "money":
                    if (GameState.Instance != null)
                    {
                        GameState.Instance.AddMoney(effect.amount);
                        Debug.Log($"[DialogueSystem] 金钱变化: {(effect.amount >= 0 ? "+" : "")}{effect.amount}");
                    }
                    break;

                default:
                    Debug.LogWarning($"[DialogueSystem] 未知效果类型: {effect.type}");
                    break;
            }
        }
    }

    // ====================================================================
    //  旧模式兼容
    // ====================================================================

    private void LegacyNextLine()
    {
        legacyLineIndex++;

        if (legacyLineIndex < legacyLines.Length)
        {
            ShowLine(legacyLines[legacyLineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    // ====================================================================
    //  NPCEventHub 对话请求处理
    // ====================================================================

    /// <summary>
    /// 处理 NPCEventHub 发来的对话请求
    /// </summary>
    private void HandleDialogueRequested(NPCEventHub.DialogueRequest request)
    {
        if (request == null || request.lines == null || request.lines.Length == 0) return;

        // 使用旧模式接口启动对话
        StartDialogue(request.speakerName, request.lines);

        // 若 portraitId 不为空，尝试加载头像
        if (!string.IsNullOrEmpty(request.portraitId))
        {
            Sprite portrait = Resources.Load<Sprite>(request.portraitId);
            if (portrait != null && uiBuilder != null && uiBuilder.portraitImage != null)
            {
                uiBuilder.portraitImage.sprite = portrait;
                uiBuilder.portraitImage.color = Color.white;
            }
        }
    }

    // ====================================================================
    //  IDialogueTrigger 接口实现（供事件系统调用）
    // ====================================================================

    /// <summary>
    /// 事件系统调用：显示一段对话，结束后回调
    /// </summary>
    void IDialogueTrigger.ShowDialogue(string speakerName, string[] lines, Action onComplete)
    {
        // 存储完成回调
        eventDialogueCompleteCallback = onComplete;

        // 使用旧模式接口播放（它天然支持 speaker + lines）
        StartDialogue(speakerName, lines);
    }

    /// <summary>
    /// 事件系统调用：显示选项按钮，玩家选择后回调索引
    /// </summary>
    void IDialogueTrigger.ShowChoices(EventChoice[] choices, Action<int> onChoiceSelected)
    {
        if (choices == null || choices.Length == 0)
        {
            onChoiceSelected?.Invoke(-1);
            return;
        }

        eventChoices = choices;
        eventChoiceCallback = onChoiceSelected;
        currentState = DialogueState.ShowingChoices;

        int choiceCount = Mathf.Min(choices.Length, 4);

        // 显示选项面板
        uiBuilder.choicePanel.SetActive(true);

        for (int i = 0; i < 4; i++)
        {
            if (i < choiceCount)
            {
                uiBuilder.choiceButtons[i].gameObject.SetActive(true);
                uiBuilder.choiceTexts[i].text = choices[i].text;
                uiBuilder.choiceButtons[i].interactable = true;
                uiBuilder.choiceTexts[i].color = Color.white;

                // 隐藏条件提示
                if (uiBuilder.choiceHints[i] != null)
                    uiBuilder.choiceHints[i].gameObject.SetActive(false);

                // 绑定点击事件
                int capturedIndex = i;
                uiBuilder.choiceButtons[i].onClick.RemoveAllListeners();
                uiBuilder.choiceButtons[i].onClick.AddListener(() =>
                {
                    OnEventChoiceSelected(capturedIndex);
                });
            }
            else
            {
                uiBuilder.choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 事件系统选项被选中后的回调
    /// </summary>
    private void OnEventChoiceSelected(int index)
    {
        // 隐藏选项面板
        uiBuilder.choicePanel.SetActive(false);
        currentState = DialogueState.Idle;

        // 清除 UI 状态
        uiBuilder.dialoguePanel.SetActive(false);

        // 回调事件系统
        Action<int> callback = eventChoiceCallback;
        eventChoiceCallback = null;
        eventChoices = null;

        callback?.Invoke(index);
    }

    // ====================================================================
    //  对话结束
    // ====================================================================

    private void EndDialogue()
    {
        string dialogueId = currentDialogueId;

        // 重置状态
        currentState = DialogueState.Idle;
        currentDialogueData = null;
        currentNode = null;
        nodeMap = null;
        currentDialogueId = null;
        legacyLines = null;
        legacyLineIndex = 0;
        isLegacyMode = false;

        // 隐藏 UI
        uiBuilder.dialoguePanel.SetActive(false);
        uiBuilder.choicePanel.SetActive(false);

        // 停止协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 触发事件
        OnDialogueEnd?.Invoke(dialogueId);
        OnDialogueEnded?.Invoke();

        // 触发事件系统的对话完成回调
        Action eventCallback = eventDialogueCompleteCallback;
        eventDialogueCompleteCallback = null;
        eventCallback?.Invoke();
    }
}
