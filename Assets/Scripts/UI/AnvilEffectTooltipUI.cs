using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnvilEffectTooltipUI : MonoBehaviour
{
    private const float PanelWidth = 405f;
    private const float PanelHeight = 190f;

    private static AnvilEffectTooltipUI activeTooltip;

    private RectTransform rectTransform;
    private Image kindPillImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI kindText;
    private TextMeshProUGUI descriptionText;

    public static void Show(AnvilEffectData effectData, Canvas canvas)
    {
        if (effectData == null || canvas == null)
        {
            Hide();
            return;
        }

        EnsureTooltip(canvas);
        activeTooltip.SetContent(effectData);
        activeTooltip.gameObject.SetActive(true);
        activeTooltip.transform.SetAsLastSibling();
    }

    public static void Hide()
    {
        if (activeTooltip != null)
            activeTooltip.gameObject.SetActive(false);
    }

    private static void EnsureTooltip(Canvas canvas)
    {
        if (activeTooltip != null && activeTooltip.transform.parent == canvas.transform)
            return;

        if (activeTooltip != null)
            Destroy(activeTooltip.gameObject);

        GameObject tooltipObject = new GameObject("AnvilEffectTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AnvilEffectTooltipUI));
        tooltipObject.transform.SetParent(canvas.transform, false);

        activeTooltip = tooltipObject.GetComponent<AnvilEffectTooltipUI>();
        activeTooltip.Build();
    }

    private void Build()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-40f, -30f);
        rectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image backgroundImage = GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.93f);
        backgroundImage.raycastTarget = false;

        titleText = CreateText("Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -64f), new Vector2(-24f, -16f), 30f, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Left;

        kindPillImage = CreatePillBackground("KindPill", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -76f), new Vector2(170f, 34f));
        kindText = CreateText("KindText", kindPillImage.rectTransform, 18f, FontStyles.Bold);

        descriptionText = CreateText("Description", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(30f, 24f), new Vector2(-24f, -118f), 21f, FontStyles.Bold);
        descriptionText.color = new Color(1f, 0.76f, 0.74f, 1f);
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Ellipsis;

        gameObject.SetActive(false);
    }

    private void SetContent(AnvilEffectData effectData)
    {
        string description = effectData.GetDescription();

        titleText.text = effectData.GetDisplayName();
        kindText.text = GameTextLocalizer.LocalizeAnvilEffectKind(effectData.kind);
        kindPillImage.color = effectData.kind == AnvilEffectKind.Blessing
            ? new Color(0.56f, 0.78f, 0.66f, 0.96f)
            : new Color(0.92f, 0.32f, 0.28f, 0.96f);
        descriptionText.text = string.IsNullOrWhiteSpace(description)
            ? GameTextLocalizer.LocalizeNoDescription()
            : description;
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.offsetMin = offsetMin;
        textRect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = new Color(0.96f, 0.96f, 0.86f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, fontSize - 8f);
        text.fontSizeMax = fontSize;
        text.margin = Vector4.zero;
        return text;
    }

    private TextMeshProUGUI CreateText(string objectName, RectTransform parent, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 2f);
        textRect.offsetMax = new Vector2(-12f, -2f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = new Color(0.08f, 0.1f, 0.07f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = fontSize;
        return text;
    }

    private Image CreatePillBackground(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject pillObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pillObject.transform.SetParent(transform, false);

        RectTransform pillRect = pillObject.GetComponent<RectTransform>();
        pillRect.anchorMin = anchorMin;
        pillRect.anchorMax = anchorMax;
        pillRect.pivot = new Vector2(0f, 1f);
        pillRect.anchoredPosition = anchoredPosition;
        pillRect.sizeDelta = sizeDelta;

        Image image = pillObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
}
