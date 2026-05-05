using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventPresentationController : MonoBehaviour
{
    private const int CanvasSortOrder = 190;

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.38f);
    private static readonly Color PlaceholderColor = new Color(0.18f, 0.20f, 0.26f, 0.94f);
    private static readonly Color PlaceholderAccent = new Color(0.86f, 0.62f, 0.24f, 0.92f);
    private static readonly Color SceneTagColor = new Color(0.95f, 0.96f, 0.98f, 0.92f);

    private Canvas presentationCanvas;
    private GameObject root;
    private Image backgroundImage;
    private TextMeshProUGUI sceneTitleText;
    private Image protagonistImage;
    private Image npcImage;
    private TextMeshProUGUI protagonistPlaceholderText;
    private TextMeshProUGUI npcPlaceholderText;
    private TextMeshProUGUI backgroundPlaceholderText;

    public bool IsVisible => root != null && root.activeSelf;

    public void EnsureBuilt()
    {
        if (root != null)
        {
            return;
        }

        root = new GameObject("EventPresentationCanvas");
        root.transform.SetParent(transform, false);

        presentationCanvas = root.AddComponent<Canvas>();
        presentationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        presentationCanvas.sortingOrder = CanvasSortOrder;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = root.GetComponent<RectTransform>();

        GameObject background = CreateUI("Background", canvasRT);
        Stretch(background.GetComponent<RectTransform>());
        backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = PlaceholderColor;

        GameObject overlay = CreateUI("Overlay", canvasRT);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = OverlayColor;

        GameObject sceneTag = CreateUI("SceneTag", canvasRT);
        RectTransform tagRT = sceneTag.GetComponent<RectTransform>();
        tagRT.anchorMin = new Vector2(0.03f, 0.92f);
        tagRT.anchorMax = new Vector2(0.40f, 0.98f);
        tagRT.offsetMin = Vector2.zero;
        tagRT.offsetMax = Vector2.zero;
        sceneTitleText = sceneTag.AddComponent<TextMeshProUGUI>();
        sceneTitleText.fontSize = 26f;
        sceneTitleText.color = SceneTagColor;
        sceneTitleText.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(sceneTitleText);

        backgroundPlaceholderText = CreatePlaceholderLabel(canvasRT, "BackgroundPlaceholder",
            new Vector2(0.22f, 0.46f), new Vector2(0.78f, 0.58f), 28f);

        protagonistImage = CreatePortraitImage(canvasRT, "ProtagonistPortrait", new Vector2(0.03f, 0.14f), new Vector2(0.30f, 0.88f), out protagonistPlaceholderText);
        npcImage = CreatePortraitImage(canvasRT, "NpcPortrait", new Vector2(0.70f, 0.14f), new Vector2(0.97f, 0.88f), out npcPlaceholderText);

        root.SetActive(false);
    }

    public void Show(EventPresentationDefinition presentation, string fallbackSpeaker, string fallbackPortraitId)
    {
        EnsureBuilt();

        if (presentation == null)
        {
            Hide();
            return;
        }

        root.SetActive(true);

        string sceneName = !string.IsNullOrWhiteSpace(presentation.sceneDisplayName)
            ? presentation.sceneDisplayName
            : (!string.IsNullOrWhiteSpace(presentation.sceneKey) ? presentation.sceneKey : "剧情场景");
        sceneTitleText.text = string.IsNullOrWhiteSpace(presentation.locationId)
            ? sceneName
            : $"{sceneName}  ·  {presentation.locationId}";

        ApplyBackground(presentation.backgroundResourcePath, presentation.backgroundSlotName);
        ApplyPortrait(protagonistImage, protagonistPlaceholderText,
            presentation.protagonistPortraitResourcePath,
            string.IsNullOrWhiteSpace(presentation.protagonistSlotName) ? "主角立绘占位" : presentation.protagonistSlotName);

        string npcPortrait = !string.IsNullOrWhiteSpace(presentation.npcPortraitResourcePath)
            ? presentation.npcPortraitResourcePath
            : fallbackPortraitId;
        string npcSlot = string.IsNullOrWhiteSpace(presentation.npcSlotName)
            ? $"{(string.IsNullOrWhiteSpace(fallbackSpeaker) ? "NPC" : fallbackSpeaker)}立绘占位"
            : presentation.npcSlotName;
        ApplyPortrait(npcImage, npcPlaceholderText, npcPortrait, npcSlot);
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void ApplyBackground(string resourcePath, string slotName)
    {
        Sprite sprite = !string.IsNullOrWhiteSpace(resourcePath) ? Resources.Load<Sprite>(resourcePath) : null;
        backgroundImage.sprite = sprite;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
        backgroundImage.color = sprite != null ? Color.white : PlaceholderColor;
        backgroundPlaceholderText.gameObject.SetActive(sprite == null);
        backgroundPlaceholderText.text = sprite != null
            ? string.Empty
            : BuildPlaceholderText("背景资源待挂载", slotName, resourcePath);
    }

    private void ApplyPortrait(Image image, TextMeshProUGUI placeholder, string resourcePath, string slotName)
    {
        Sprite sprite = !string.IsNullOrWhiteSpace(resourcePath) ? Resources.Load<Sprite>(resourcePath) : null;
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : PlaceholderColor;
        placeholder.gameObject.SetActive(sprite == null);
        placeholder.text = sprite != null
            ? string.Empty
            : BuildPlaceholderText("立绘资源待挂载", slotName, resourcePath);
    }

    private string BuildPlaceholderText(string title, string slotName, string resourcePath)
    {
        string resolvedSlot = string.IsNullOrWhiteSpace(slotName) ? "未命名占位" : slotName;
        string resolvedPath = string.IsNullOrWhiteSpace(resourcePath) ? "Resources 路径待填写" : resourcePath;
        return $"{title}\n{resolvedSlot}\n{resolvedPath}";
    }

    private Image CreatePortraitImage(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI placeholderText)
    {
        GameObject portrait = CreateUI(name, parent);
        RectTransform portraitRT = portrait.GetComponent<RectTransform>();
        portraitRT.anchorMin = anchorMin;
        portraitRT.anchorMax = anchorMax;
        portraitRT.offsetMin = Vector2.zero;
        portraitRT.offsetMax = Vector2.zero;

        Image image = portrait.AddComponent<Image>();
        image.preserveAspect = true;
        image.color = PlaceholderColor;

        GameObject border = CreateUI("Border", portraitRT);
        Stretch(border.GetComponent<RectTransform>());
        Outline outline = border.AddComponent<Outline>();
        outline.effectColor = PlaceholderAccent;
        outline.effectDistance = new Vector2(2f, -2f);

        placeholderText = CreatePlaceholderLabel(portraitRT, name + "Placeholder", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.28f), 22f);
        placeholderText.alignment = TextAlignmentOptions.Center;
        return image;
    }

    private TextMeshProUGUI CreatePlaceholderLabel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
    {
        GameObject textObject = CreateUI(name, parent);
        RectTransform textRT = textObject.GetComponent<RectTransform>();
        textRT.anchorMin = anchorMin;
        textRT.anchorMax = anchorMax;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = PlaceholderAccent;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        ApplyFont(text);
        return text;
    }

    private GameObject CreateUI(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }
    }
}
