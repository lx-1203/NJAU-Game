using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 对话系统 - 单例模式
/// 管理对话框UI的创建、显示、文字逐字显示、翻页
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("对话设置")]
    [SerializeField] private float textSpeed = 0.04f; // 每个字的显示间隔

    // UI 组件引用
    private Canvas dialogueCanvas;
    private GameObject dialoguePanel;
    private Text nameText;
    private Text contentText;
    private Text hintText;
    private Image portraitImage;

    // 对话状态
    private string[] currentLines;
    private int currentLineIndex;
    private bool isTyping = false;
    private bool isDialogueActive = false;
    private string currentFullText;
    private Coroutine typingCoroutine;

    public bool IsDialogueActive => isDialogueActive;

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateDialogueUI();
    }

    private void CreateDialogueUI()
    {
        // ===== 创建对话专用 Canvas =====
        GameObject canvasObj = new GameObject("DialogueCanvas");
        canvasObj.transform.SetParent(transform);
        dialogueCanvas = canvasObj.AddComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ===== 对话面板（底部） =====
        dialoguePanel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialoguePanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRt = dialoguePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.05f, 0.02f);
        panelRt.anchorMax = new Vector2(0.95f, 0.32f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelImg = dialoguePanel.GetComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.12f, 0.92f);

        // ===== 角色头像 =====
        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portraitObj.transform.SetParent(dialoguePanel.transform, false);

        RectTransform portraitRt = portraitObj.GetComponent<RectTransform>();
        portraitRt.anchorMin = new Vector2(0, 0.1f);
        portraitRt.anchorMax = new Vector2(0, 0.9f);
        portraitRt.offsetMin = new Vector2(15, 0);
        portraitRt.offsetMax = new Vector2(15, 0);
        portraitRt.sizeDelta = new Vector2(130, 0);
        portraitRt.anchoredPosition = new Vector2(80, 0);

        portraitImage = portraitObj.GetComponent<Image>();
        portraitImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        portraitImage.preserveAspect = true;

        // ===== NPC 名字 =====
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        nameObj.transform.SetParent(dialoguePanel.transform, false);

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.12f, 0.75f);
        nameRt.anchorMax = new Vector2(0.5f, 0.95f);
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;

        nameText = nameObj.GetComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 30;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = new Color(0.4f, 0.85f, 1f);
        nameText.alignment = TextAnchor.MiddleLeft;

        // ===== 名字底部装饰线 =====
        GameObject lineObj = new GameObject("NameLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObj.transform.SetParent(dialoguePanel.transform, false);

        RectTransform lineRt = lineObj.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0.12f, 0.73f);
        lineRt.anchorMax = new Vector2(0.95f, 0.74f);
        lineRt.offsetMin = Vector2.zero;
        lineRt.offsetMax = Vector2.zero;

        lineObj.GetComponent<Image>().color = new Color(0.4f, 0.85f, 1f, 0.3f);

        // ===== 对话内容 =====
        GameObject contentObj = new GameObject("ContentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        contentObj.transform.SetParent(dialoguePanel.transform, false);

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.12f, 0.08f);
        contentRt.anchorMax = new Vector2(0.95f, 0.7f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        contentText = contentObj.GetComponent<Text>();
        contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        contentText.fontSize = 26;
        contentText.color = Color.white;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.lineSpacing = 1.3f;

        // ===== 底部提示文字 =====
        GameObject hintObj = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        hintObj.transform.SetParent(dialoguePanel.transform, false);

        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.7f, 0.02f);
        hintRt.anchorMax = new Vector2(0.98f, 0.15f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;

        hintText = hintObj.GetComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 18;
        hintText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        hintText.alignment = TextAnchor.MiddleRight;
        hintText.text = "按 空格键 继续...";

        // 默认隐藏
        dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// 开始一段对话
    /// </summary>
    public void StartDialogue(string speakerName, string[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        currentLines = lines;
        currentLineIndex = 0;
        isDialogueActive = true;

        // 设置名字
        nameText.text = speakerName;

        // 尝试加载NPC头像
        Sprite npcSprite = Resources.Load<Sprite>("NPCSprite");
        if (npcSprite != null)
        {
            portraitImage.sprite = npcSprite;
            portraitImage.color = Color.white;
        }

        dialoguePanel.SetActive(true);

        // 显示第一句
        ShowLine(currentLines[currentLineIndex]);
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // 按空格键或鼠标左键推进对话
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // 正在打字 → 立即显示完整文字
                SkipTyping();
            }
            else
            {
                // 已经显示完 → 下一句
                NextLine();
            }
        }
    }

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
        isTyping = true;
        contentText.text = "";
        hintText.text = "▼";

        foreach (char c in text)
        {
            contentText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;

        // 更新提示
        if (currentLineIndex < currentLines.Length - 1)
        {
            hintText.text = "按 空格键 继续...";
        }
        else
        {
            hintText.text = "按 空格键 结束";
        }
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        contentText.text = currentFullText;
        isTyping = false;

        if (currentLineIndex < currentLines.Length - 1)
        {
            hintText.text = "按 空格键 继续...";
        }
        else
        {
            hintText.text = "按 空格键 结束";
        }
    }

    private void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentLines.Length)
        {
            ShowLine(currentLines[currentLineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        currentLines = null;
        currentLineIndex = 0;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }
}
