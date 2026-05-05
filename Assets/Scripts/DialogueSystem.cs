using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueSystem : MonoBehaviour, IDialogueTrigger, ISaveable
{
    public static DialogueSystem Instance { get; private set; }

    private enum DialogueState
    {
        Idle,
        ShowingText,
        WaitingForInput,
        ShowingChoices
    }

    private struct DialogueHistoryEntry
    {
        public bool isLegacy;
        public string nodeId;
        public int legacyIndex;
    }

    private enum SkipPermission
    {
        Blocked,
        CompleteCurrentLineOnly,
        AllowAdvance
    }

    [Header("Dialogue Settings")]
    [SerializeField] private float textSpeed = 0.04f;
    [SerializeField] private float[] skipAdvanceIntervals = { 0.25f, 0.1f, 0.05f, 0.033f };

    public event Action<string> OnDialogueStart;
    public event Action<string> OnDialogueEnd;
    public event Action OnDialogueEnded;
    public event Action<DialogueChoice> OnChoiceMade;

    public bool IsDialogueActive => currentState != DialogueState.Idle;
    public bool CanStepBack => IsDialogueActive && history.Count > 0;

    bool IDialogueTrigger.IsActive => IsDialogueActive;

    private DialogueState currentState = DialogueState.Idle;
    private DialogueUIBuilder uiBuilder;
    private EventPresentationController eventPresentationController;

    private DialogueData currentDialogueData;
    private Dictionary<string, DialogueNode> nodeMap;
    private DialogueNode currentNode;
    private string currentDialogueId;

    private string[] legacyLines;
    private int legacyLineIndex;
    private bool isLegacyMode;

    private string currentFullText;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private float nextSkipAdvanceTime;
    private bool currentLineWasSeenBefore;

    private readonly Stack<DialogueHistoryEntry> history = new Stack<DialogueHistoryEntry>();
    private readonly HashSet<string> seenDialogueEntries = new HashSet<string>();

    private Action eventDialogueCompleteCallback;
    private Action<int> eventChoiceCallback;
    private EventChoice[] eventChoices;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        uiBuilder = gameObject.AddComponent<DialogueUIBuilder>();
        uiBuilder.BuildDialogueUI();
        eventPresentationController = gameObject.AddComponent<EventPresentationController>();
        eventPresentationController.EnsureBuilt();
        if (uiBuilder.previousButton != null)
        {
            uiBuilder.previousButton.onClick.RemoveAllListeners();
            uiBuilder.previousButton.onClick.AddListener(DebugStepBackOneLine);
        }
    }

    private void Start()
    {
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
        if (!IsDialogueActive)
        {
            return;
        }

        if (PauseMenuUI.IsBlockingUnderlyingInput)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            HandleInput();
            return;
        }

        if (IsSkipHeld())
        {
            HandleSkipInput();
        }
    }

    public void StartDialogue(string dialogueId)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning("[DialogueSystem] A dialogue is already playing.");
            ShowDialogueNotification("对话进行中", "当前还有对话没有结束。", new Color(0.86f, 0.62f, 0.24f), 2.5f);
            return;
        }

        DialogueData data = DialogueParser.GetDialogue(dialogueId);
        if (data == null)
        {
            Debug.LogWarning($"[DialogueSystem] Dialogue not found: {dialogueId}");
            ShowDialogueNotification("对话未找到", $"编号为 {dialogueId} 的对话数据暂时不存在。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return;
        }

        if (data.nodes == null || data.nodes.Length == 0)
        {
            Debug.LogWarning($"[DialogueSystem] Dialogue has no nodes: {dialogueId}");
            ShowDialogueNotification("对话为空", $"“{dialogueId}”还没有可播放内容。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return;
        }

        ResetRuntimeState();
        isLegacyMode = false;
        currentDialogueData = data;
        currentDialogueId = dialogueId;

        nodeMap = new Dictionary<string, DialogueNode>();
        foreach (DialogueNode node in data.nodes)
        {
            if (!string.IsNullOrEmpty(node.id))
            {
                nodeMap[node.id] = node;
            }
        }

        uiBuilder.dialoguePanel.SetActive(true);
        uiBuilder.choicePanel.SetActive(false);
        OnDialogueStart?.Invoke(dialogueId);

        NavigateToNode(data.nodes[0].id, false);
    }

    public void StartDialogue(string speakerName, string[] lines)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning("[DialogueSystem] A dialogue is already playing.");
            ShowDialogueNotification("对话进行中", "当前还有对话没有结束。", new Color(0.86f, 0.62f, 0.24f), 2.5f);
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            return;
        }

        ResetRuntimeState();
        isLegacyMode = true;
        legacyLines = lines;
        legacyLineIndex = 0;
        currentDialogueId = speakerName;

        uiBuilder.dialoguePanel.SetActive(true);
        uiBuilder.choicePanel.SetActive(false);
        uiBuilder.nameText.text = speakerName;
        uiBuilder.nameContainer.SetActive(true);
        uiBuilder.contentText.fontStyle = FontStyles.Normal;
        uiBuilder.contentText.color = Color.white;
        uiBuilder.portraitContainer.SetActive(true);
        ResetContentAnchors(false);
        SetPortraitPlaceholder();

        OnDialogueStart?.Invoke(speakerName);
        ShowLine(legacyLines[legacyLineIndex]);
    }

    public void SetTextSpeed(float speed)
    {
        textSpeed = Mathf.Max(0.001f, speed);
    }

    public void ApplyEventPresentation(EventPresentationDefinition presentation, string fallbackSpeaker, string fallbackPortraitId)
    {
        if (eventPresentationController == null)
        {
            eventPresentationController = gameObject.GetComponent<EventPresentationController>();
            if (eventPresentationController == null)
            {
                eventPresentationController = gameObject.AddComponent<EventPresentationController>();
            }
        }

        eventPresentationController.EnsureBuilt();
        eventPresentationController.Show(presentation, fallbackSpeaker, fallbackPortraitId);
    }

    public void ClearEventPresentation()
    {
        if (eventPresentationController != null)
        {
            eventPresentationController.Hide();
        }
    }

    public void DebugStepBackOneLine()
    {
        if (!CanStepBack)
        {
            return;
        }

        if (isTyping)
        {
            SkipTyping();
        }

        DialogueHistoryEntry entry = history.Pop();
        if (entry.isLegacy)
        {
            RestoreLegacyLine(entry.legacyIndex);
        }
        else
        {
            RestoreNode(entry.nodeId);
        }

        RefreshPreviousButton();
    }

    private void HandleInput()
    {
        switch (currentState)
        {
            case DialogueState.ShowingText:
                SkipTyping();
                break;
            case DialogueState.WaitingForInput:
                if (isLegacyMode)
                {
                    LegacyNextLine();
                }
                else if (currentNode != null && currentNode.choices != null && currentNode.choices.Length > 0)
                {
                    ShowChoices();
                }
                else if (currentNode != null && !string.IsNullOrEmpty(currentNode.next))
                {
                    NavigateToNode(currentNode.next, true);
                }
                else
                {
                    EndDialogue();
                }
                break;
            case DialogueState.ShowingChoices:
                break;
        }
    }

    private void HandleSkipInput()
    {
        if (currentState == DialogueState.ShowingChoices)
        {
            return;
        }

        SkipPermission permission = GetSkipPermission();
        if (permission == SkipPermission.Blocked)
        {
            return;
        }

        if (currentState == DialogueState.ShowingText)
        {
            SkipTyping();
            if (permission == SkipPermission.AllowAdvance)
            {
                nextSkipAdvanceTime = Time.unscaledTime + GetSkipAdvanceInterval();
            }
            return;
        }

        if (permission != SkipPermission.AllowAdvance)
        {
            return;
        }

        if (currentState != DialogueState.WaitingForInput || Time.unscaledTime < nextSkipAdvanceTime)
        {
            return;
        }

        HandleInput();
        nextSkipAdvanceTime = Time.unscaledTime + GetSkipAdvanceInterval();
    }

    private bool IsSkipHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private float GetSkipAdvanceInterval()
    {
        int speedIndex = 2;
        if (SettingsManager.Instance != null && SettingsManager.Instance.CurrentSettings != null)
        {
            speedIndex = SettingsManager.Instance.CurrentSettings.fastForwardSpeed;
        }

        if (skipAdvanceIntervals == null || skipAdvanceIntervals.Length == 0)
        {
            return 0.05f;
        }

        speedIndex = Mathf.Clamp(speedIndex, 0, skipAdvanceIntervals.Length - 1);
        return Mathf.Max(0.01f, skipAdvanceIntervals[speedIndex]);
    }

    private SkipPermission GetSkipPermission()
    {
        if (!IsSkipHeld() || currentState == DialogueState.ShowingChoices)
        {
            return SkipPermission.Blocked;
        }

        if (!ShouldRestrictSkipToSeenEntries())
        {
            return SkipPermission.AllowAdvance;
        }

        return currentLineWasSeenBefore
            ? SkipPermission.AllowAdvance
            : SkipPermission.CompleteCurrentLineOnly;
    }

    private bool ShouldRestrictSkipToSeenEntries()
    {
        return SettingsManager.Instance != null
            && SettingsManager.Instance.CurrentSettings != null
            && SettingsManager.Instance.CurrentSettings.skipMode == 1;
    }

    private string GetCurrentDialogueEntryKey()
    {
        if (isLegacyMode)
        {
            return $"legacy:{currentDialogueId}:{legacyLineIndex}";
        }

        if (currentNode != null && !string.IsNullOrEmpty(currentNode.id))
        {
            return $"node:{currentDialogueId}:{currentNode.id}";
        }

        return null;
    }

    private void UpdateCurrentLineSeenState()
    {
        string entryKey = GetCurrentDialogueEntryKey();
        currentLineWasSeenBefore = !string.IsNullOrEmpty(entryKey) && seenDialogueEntries.Contains(entryKey);
    }

    private void MarkCurrentLineAsSeen()
    {
        string entryKey = GetCurrentDialogueEntryKey();
        if (string.IsNullOrEmpty(entryKey))
        {
            currentLineWasSeenBefore = false;
            return;
        }

        seenDialogueEntries.Add(entryKey);
        currentLineWasSeenBefore = true;
    }

    private void NavigateToNode(string nodeId, bool recordHistory)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            EndDialogue();
            return;
        }

        if (nodeMap == null || !nodeMap.TryGetValue(nodeId, out DialogueNode node))
        {
            Debug.LogError($"[DialogueSystem] Missing node: {nodeId}");
            ShowDialogueNotification("对话中断", $"对话节点 {nodeId} 缺失，当前对话已结束。", new Color(0.82f, 0.38f, 0.30f), 3f);
            EndDialogue();
            return;
        }

        if (recordHistory && currentNode != null && !string.IsNullOrEmpty(currentNode.id))
        {
            history.Push(new DialogueHistoryEntry
            {
                isLegacy = false,
                nodeId = currentNode.id
            });
        }

        currentNode = node;
        uiBuilder.choicePanel.SetActive(false);
        ConfigureNodeUI(node);
        ShowLine(node.content);
        RefreshPreviousButton();
    }

    private void RestoreNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || nodeMap == null || !nodeMap.TryGetValue(nodeId, out DialogueNode node))
        {
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isLegacyMode = false;
        currentNode = node;
        currentFullText = node.content ?? string.Empty;
        UpdateCurrentLineSeenState();
        isTyping = false;
        currentState = DialogueState.WaitingForInput;

        uiBuilder.dialoguePanel.SetActive(true);
        uiBuilder.choicePanel.SetActive(false);
        ConfigureNodeUI(node);
        uiBuilder.contentText.text = currentFullText;
        UpdateHintAfterTextComplete();
    }

    private void RestoreLegacyLine(int lineIndex)
    {
        if (legacyLines == null || lineIndex < 0 || lineIndex >= legacyLines.Length)
        {
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isLegacyMode = true;
        legacyLineIndex = lineIndex;
        currentFullText = legacyLines[legacyLineIndex] ?? string.Empty;
        UpdateCurrentLineSeenState();
        isTyping = false;
        currentState = DialogueState.WaitingForInput;

        uiBuilder.dialoguePanel.SetActive(true);
        uiBuilder.choicePanel.SetActive(false);
        uiBuilder.contentText.text = currentFullText;
        UpdateHintAfterTextComplete();
    }

    private void ConfigureNodeUI(DialogueNode node)
    {
        bool isNarrator = string.IsNullOrEmpty(node.speaker);
        bool isInner = node.speaker == "_inner";

        if (isNarrator)
        {
            uiBuilder.portraitContainer.SetActive(false);
            uiBuilder.nameContainer.SetActive(false);
            uiBuilder.contentText.fontStyle = FontStyles.Normal;
            uiBuilder.contentText.color = new Color(0.85f, 0.85f, 0.75f);
            ResetContentAnchors(true);
        }
        else if (isInner)
        {
            uiBuilder.portraitContainer.SetActive(false);
            uiBuilder.nameContainer.SetActive(false);
            uiBuilder.contentText.fontStyle = FontStyles.Italic;
            uiBuilder.contentText.color = new Color(0.75f, 0.75f, 0.85f);
            ResetContentAnchors(true);
        }
        else
        {
            uiBuilder.portraitContainer.SetActive(true);
            uiBuilder.nameContainer.SetActive(true);
            uiBuilder.nameText.text = node.speaker;
            uiBuilder.contentText.fontStyle = FontStyles.Normal;
            uiBuilder.contentText.color = Color.white;
            ResetContentAnchors(false);

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
                    SetPortraitPlaceholder();
                }
            }
            else
            {
                SetPortraitPlaceholder();
            }
        }
    }

    private void ResetContentAnchors(bool wideMode)
    {
        RectTransform rect = uiBuilder.contentText.rectTransform;
        rect.anchorMin = wideMode ? new Vector2(0.05f, 0.08f) : new Vector2(0.12f, 0.08f);
    }

    private void SetPortraitPlaceholder()
    {
        if (uiBuilder == null || uiBuilder.portraitImage == null)
        {
            return;
        }

        uiBuilder.portraitImage.sprite = null;
        uiBuilder.portraitImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
    }

    private void ShowLine(string line)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        currentFullText = line ?? string.Empty;
        UpdateCurrentLineSeenState();
        typingCoroutine = StartCoroutine(TypeText(currentFullText));
    }

    private IEnumerator TypeText(string text)
    {
        currentState = DialogueState.ShowingText;
        isTyping = true;
        uiBuilder.contentText.text = string.Empty;
        uiBuilder.hintText.text = "...";

        foreach (char character in text)
        {
            uiBuilder.contentText.text += character;
            yield return new WaitForSeconds(textSpeed);
        }

        typingCoroutine = null;
        isTyping = false;
        OnTextComplete();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        uiBuilder.contentText.text = currentFullText;
        isTyping = false;
        OnTextComplete();
    }

    private void OnTextComplete()
    {
        MarkCurrentLineAsSeen();
        UpdateHintAfterTextComplete();
        RefreshPreviousButton();
    }

    private void UpdateHintAfterTextComplete()
    {
        if (isLegacyMode)
        {
            uiBuilder.hintText.text = legacyLineIndex < legacyLines.Length - 1
                ? "Space / Click / Hold Ctrl to continue"
                : "Space / Click / Hold Ctrl to finish";
            currentState = DialogueState.WaitingForInput;
            return;
        }

        if (currentNode != null && currentNode.choices != null && currentNode.choices.Length > 0)
        {
            uiBuilder.hintText.text = string.Empty;
            ShowChoices();
            return;
        }

        uiBuilder.hintText.text = currentNode != null && !string.IsNullOrEmpty(currentNode.next)
            ? "Space / Click / Hold Ctrl to continue"
            : "Space / Click / Hold Ctrl to finish";
        currentState = DialogueState.WaitingForInput;
    }

    private void ShowChoices()
    {
        if (currentNode == null || currentNode.choices == null)
        {
            return;
        }

        currentState = DialogueState.ShowingChoices;
        uiBuilder.choicePanel.SetActive(true);

        int choiceCount = Mathf.Min(currentNode.choices.Length, uiBuilder.choiceButtons.Length);
        for (int i = 0; i < uiBuilder.choiceButtons.Length; i++)
        {
            if (i >= choiceCount)
            {
                uiBuilder.choiceButtons[i].gameObject.SetActive(false);
                continue;
            }

            DialogueChoice choice = currentNode.choices[i];
            bool conditionMet = DialogueParser.EvaluateCondition(choice.condition);

            uiBuilder.choiceButtons[i].gameObject.SetActive(true);
            uiBuilder.choiceButtons[i].interactable = conditionMet;
            uiBuilder.choiceTexts[i].text = choice.text;
            uiBuilder.choiceTexts[i].color = conditionMet ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            uiBuilder.choiceButtons[i].onClick.RemoveAllListeners();

            if (!string.IsNullOrEmpty(choice.conditionHint) && !conditionMet)
            {
                uiBuilder.choiceHints[i].text = choice.conditionHint;
                uiBuilder.choiceHints[i].gameObject.SetActive(true);
            }
            else
            {
                uiBuilder.choiceHints[i].gameObject.SetActive(false);
            }

            int capturedIndex = i;
            uiBuilder.choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(capturedIndex));
        }
    }

    private void OnChoiceSelected(int index)
    {
        if (currentNode == null || currentNode.choices == null || index < 0 || index >= currentNode.choices.Length)
        {
            return;
        }

        DialogueChoice choice = currentNode.choices[index];
        if (choice.effects != null && choice.effects.Length > 0)
        {
            ApplyEffects(choice.effects);
        }

        OnChoiceMade?.Invoke(choice);
        uiBuilder.choicePanel.SetActive(false);

        if (!string.IsNullOrEmpty(choice.next))
        {
            if (currentNode != null && !string.IsNullOrEmpty(currentNode.id))
            {
                history.Push(new DialogueHistoryEntry
                {
                    isLegacy = false,
                    nodeId = currentNode.id
                });
            }

            NavigateToNode(choice.next, false);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ApplyEffects(DialogueEffect[] effects)
    {
        foreach (DialogueEffect effect in effects)
        {
            switch (effect.type)
            {
                case "attribute":
                    if (PlayerAttributes.Instance != null)
                    {
                        PlayerAttributes.Instance.AddAttribute(effect.target, effect.amount);
                    }
                    break;
                case "money":
                    if (GameState.Instance != null)
                    {
                        GameState.Instance.AddMoney(effect.amount);
                    }
                    break;
                default:
                    Debug.LogWarning($"[DialogueSystem] Unknown effect type: {effect.type}");
                    ShowDialogueNotification("对话效果未识别", $"检测到未识别的效果类型：{effect.type}。", new Color(0.82f, 0.38f, 0.30f), 3f);
                    break;
            }
        }
    }

    private void LegacyNextLine()
    {
        history.Push(new DialogueHistoryEntry
        {
            isLegacy = true,
            legacyIndex = legacyLineIndex
        });

        legacyLineIndex++;
        if (legacyLines != null && legacyLineIndex < legacyLines.Length)
        {
            ShowLine(legacyLines[legacyLineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    private void HandleDialogueRequested(NPCEventHub.DialogueRequest request)
    {
        if (request == null || request.lines == null || request.lines.Length == 0)
        {
            return;
        }

        StartDialogue(request.speakerName, request.lines);

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

    void IDialogueTrigger.ShowDialogue(string speakerName, string[] lines, Action onComplete)
    {
        eventDialogueCompleteCallback = onComplete;
        StartDialogue(speakerName, lines);
    }

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
        uiBuilder.choicePanel.SetActive(true);

        int choiceCount = Mathf.Min(choices.Length, uiBuilder.choiceButtons.Length);
        for (int i = 0; i < uiBuilder.choiceButtons.Length; i++)
        {
            if (i >= choiceCount)
            {
                uiBuilder.choiceButtons[i].gameObject.SetActive(false);
                continue;
            }

            uiBuilder.choiceButtons[i].gameObject.SetActive(true);
            uiBuilder.choiceButtons[i].interactable = true;
            uiBuilder.choiceTexts[i].text = choices[i].text;
            uiBuilder.choiceTexts[i].color = Color.white;
            uiBuilder.choiceHints[i].gameObject.SetActive(false);
            uiBuilder.choiceButtons[i].onClick.RemoveAllListeners();

            int capturedIndex = i;
            uiBuilder.choiceButtons[i].onClick.AddListener(() => OnEventChoiceSelected(capturedIndex));
        }
    }

    private void OnEventChoiceSelected(int index)
    {
        uiBuilder.choicePanel.SetActive(false);
        uiBuilder.dialoguePanel.SetActive(false);
        currentState = DialogueState.Idle;

        Action<int> callback = eventChoiceCallback;
        eventChoiceCallback = null;
        eventChoices = null;
        callback?.Invoke(index);
    }

    private void EndDialogue()
    {
        string dialogueId = currentDialogueId;

        currentState = DialogueState.Idle;
        currentDialogueData = null;
        currentNode = null;
        nodeMap = null;
        currentDialogueId = null;
        legacyLines = null;
        legacyLineIndex = 0;
        isLegacyMode = false;
        currentFullText = null;
        history.Clear();

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        uiBuilder.dialoguePanel.SetActive(false);
        uiBuilder.choicePanel.SetActive(false);
        RefreshPreviousButton();

        OnDialogueEnd?.Invoke(dialogueId);
        OnDialogueEnded?.Invoke();

        Action callback = eventDialogueCompleteCallback;
        eventDialogueCompleteCallback = null;
        callback?.Invoke();
    }

    private void ResetRuntimeState()
    {
        history.Clear();
        currentDialogueData = null;
        currentNode = null;
        nodeMap = null;
        currentDialogueId = null;
        legacyLines = null;
        legacyLineIndex = 0;
        currentFullText = null;
        isLegacyMode = false;
        isTyping = false;
        nextSkipAdvanceTime = 0f;
        currentLineWasSeenBefore = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        uiBuilder.hintText.text = string.Empty;
        RefreshPreviousButton();
    }

    public int GetSeenDialogueEntryCount()
    {
        return seenDialogueEntries.Count;
    }

    public void ClearSeenDialogueEntries()
    {
        seenDialogueEntries.Clear();
        UpdateCurrentLineSeenState();
    }

    public void SaveToData(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.seenDialogueEntries = new List<string>(seenDialogueEntries);
    }

    public void LoadFromData(SaveData data)
    {
        seenDialogueEntries.Clear();
        if (data != null && data.seenDialogueEntries != null)
        {
            foreach (string entry in data.seenDialogueEntries)
            {
                if (!string.IsNullOrEmpty(entry))
                {
                    seenDialogueEntries.Add(entry);
                }
            }
        }

        UpdateCurrentLineSeenState();
    }

    private void RefreshPreviousButton()
    {
        if (uiBuilder == null || uiBuilder.previousButton == null)
        {
            return;
        }

        bool showButton = IsDialogueActive;
        uiBuilder.previousButton.gameObject.SetActive(showButton);
        uiBuilder.previousButton.interactable = CanStepBack;
        if (uiBuilder.previousButtonText != null)
        {
            uiBuilder.previousButtonText.text = CanStepBack ? "Previous" : "Previous";
        }
    }

    private void ShowDialogueNotification(string title, string message, Color color, float duration)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, color, duration);
        }
    }
}
