using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    private const float PanelWidth = 405f;
    private const float PanelHeight = 255f;
    private const float StatsPanelGap = 4f;
    private const float StatsPanelPaddingX = 24f;
    private const float StatsPanelPaddingY = 18f;
    private const float StatsRowHeight = 32f;

    private static ItemTooltipUI activeTooltip;

    private RectTransform rectTransform;
    private Image rarityPillImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI rarityText;
    private TextMeshProUGUI typeText;
    private TextMeshProUGUI descriptionText;
    private Image placementRuleDetailIconImage;
    private TextMeshProUGUI placementRuleDescriptionText;
    private RectTransform statsPanel;
    private readonly List<TextMeshProUGUI> statTexts = new();

    public static void Show(ItemInstance itemInstance, Canvas canvas)
    {
        if (itemInstance == null || itemInstance.data == null || canvas == null)
        {
            Hide();
            return;
        }

        EnsureTooltip(canvas);
        activeTooltip.SetContent(itemInstance);
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
            UnityEngine.Object.Destroy(activeTooltip.gameObject);

        GameObject tooltipObject = new GameObject("ItemTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ItemTooltipUI));
        tooltipObject.transform.SetParent(canvas.transform, false);

        activeTooltip = tooltipObject.GetComponent<ItemTooltipUI>();
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

        rarityPillImage = CreatePillBackground("RarityPill", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -76f), new Vector2(170f, 34f));
        rarityText = CreateText("RarityText", rarityPillImage.rectTransform, 18f, FontStyles.Bold);

        Image typePillImage = CreatePillBackground("TypePill", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(214f, -76f), new Vector2(150f, 34f));
        typePillImage.color = new Color(0.25f, 0.25f, 0.19f, 0.94f);
        typeText = CreateText("TypeText", typePillImage.rectTransform, 18f, FontStyles.Bold);

        descriptionText = CreateText("Description", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(30f, 76f), new Vector2(-24f, -118f), 21f, FontStyles.Bold);
        descriptionText.color = new Color(1f, 0.76f, 0.74f, 1f);
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Ellipsis;

        placementRuleDetailIconImage = CreateImage("PlacementRuleDetailIcon", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -174f), new Vector2(30f, 30f));
        placementRuleDescriptionText = CreateText("PlacementRuleDescription", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(88f, 28f), new Vector2(-24f, -174f), 18f, FontStyles.Bold);
        placementRuleDescriptionText.color = new Color(0.96f, 0.96f, 0.86f, 1f);
        placementRuleDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        placementRuleDescriptionText.enableWordWrapping = true;
        placementRuleDescriptionText.overflowMode = TextOverflowModes.Ellipsis;

        gameObject.SetActive(false);
    }

    private void SetContent(ItemInstance itemInstance)
    {
        ItemData itemData = itemInstance.data;
        string description = itemData.GetDescription();

        titleText.text = itemData.GetDisplayName();
        descriptionText.text = string.IsNullOrWhiteSpace(description)
            ? GameTextLocalizer.LocalizeNoDescription()
            : description;
        rarityText.text = GameTextLocalizer.LocalizeRarity(itemInstance.rarity);
        typeText.text = GameTextLocalizer.LocalizeItemType(itemData);
        rarityPillImage.color = GetRarityColor(itemInstance.rarity);
        UpdatePlacementRule(itemInstance.placementRule);
        UpdateStatsPanel(itemData.stats);
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

    private Image CreateImage(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = anchorMin;
        imageRect.anchorMax = anchorMax;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = anchoredPosition;
        imageRect.sizeDelta = sizeDelta;

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.enabled = false;
        return image;
    }

    private void UpdatePlacementRule(GridPlacementRule rule)
    {
        Sprite ruleSprite = GridPlacementRuleUtility.LoadRuleSprite(rule);
        string ruleDescription = GameTextLocalizer.LocalizePlacementRuleDescription(rule);
        bool hasRule = rule != GridPlacementRule.None && ruleSprite != null && !string.IsNullOrWhiteSpace(ruleDescription);

        SetPlacementRuleImage(placementRuleDetailIconImage, ruleSprite, hasRule);

        if (placementRuleDescriptionText != null)
        {
            placementRuleDescriptionText.text = hasRule ? ruleDescription : string.Empty;
            placementRuleDescriptionText.gameObject.SetActive(hasRule);
        }
    }

    private void SetPlacementRuleImage(Image image, Sprite sprite, bool isVisible)
    {
        if (image == null)
            return;

        image.sprite = isVisible ? sprite : null;
        image.enabled = isVisible;
        image.gameObject.SetActive(isVisible);
    }

    private void UpdateStatsPanel(List<ItemStat> stats)
    {
        EnsureStatsPanel();
        List<ItemStat> visibleStats = GetVisibleStats(stats);

        if (visibleStats.Count == 0)
        {
            statsPanel.gameObject.SetActive(false);
            return;
        }

        int rowCount = Mathf.CeilToInt(visibleStats.Count / 2f);
        float panelHeight = StatsPanelPaddingY * 2f + rowCount * StatsRowHeight;
        statsPanel.sizeDelta = new Vector2(0f, panelHeight);
        statsPanel.gameObject.SetActive(true);

        EnsureStatTextCount(visibleStats.Count);

        for (int i = 0; i < statTexts.Count; i++)
        {
            TextMeshProUGUI text = statTexts[i];
            bool isVisible = i < visibleStats.Count;
            text.gameObject.SetActive(isVisible);

            if (!isVisible)
                continue;

            ItemStat stat = visibleStats[i];
            int row = i / 2;
            bool isLeftColumn = i % 2 == 0;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(
                isLeftColumn ? StatsPanelPaddingX : PanelWidth * 0.5f + StatsPanelPaddingX * 0.5f,
                -StatsPanelPaddingY - row * StatsRowHeight
            );
            textRect.sizeDelta = new Vector2(PanelWidth * 0.5f - StatsPanelPaddingX * 1.5f, StatsRowHeight);

            text.text = $"{GameTextLocalizer.LocalizeStatType(stat.statType)}: {FormatStatValue(stat)}";
        }
    }

    private void EnsureStatsPanel()
    {
        if (statsPanel != null)
            return;

        GameObject panelObject = new GameObject("StatsTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(transform, false);

        statsPanel = panelObject.GetComponent<RectTransform>();
        statsPanel.anchorMin = new Vector2(0f, 0f);
        statsPanel.anchorMax = new Vector2(1f, 0f);
        statsPanel.pivot = new Vector2(0.5f, 1f);
        statsPanel.anchoredPosition = new Vector2(0f, -StatsPanelGap);
        statsPanel.sizeDelta = new Vector2(0f, StatsPanelPaddingY * 2f + StatsRowHeight);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.93f);
        panelImage.raycastTarget = false;

        statsPanel.gameObject.SetActive(false);
    }

    private void EnsureStatTextCount(int count)
    {
        while (statTexts.Count < count)
        {
            GameObject textObject = new GameObject($"StatText{statTexts.Count + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(statsPanel, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.fontSize = 16f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.42f, 0.53f, 1f, 1f);
            text.alignment = TextAlignmentOptions.Left;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13f;
            text.fontSizeMax = 16f;
            text.overflowMode = TextOverflowModes.Ellipsis;
            statTexts.Add(text);
        }
    }

    private List<ItemStat> GetVisibleStats(List<ItemStat> stats)
    {
        List<ItemStat> visibleStats = new();
        if (stats == null)
            return visibleStats;

        for (int i = 0; i < stats.Count; i++)
        {
            ItemStat stat = stats[i];
            if (stat == null || Mathf.Approximately(stat.value, 0f))
                continue;

            visibleStats.Add(stat);
        }

        return visibleStats;
    }

    private string FormatStatValue(ItemStat stat)
    {
        string valueText = Mathf.Approximately(stat.value, Mathf.Round(stat.value))
            ? Mathf.RoundToInt(stat.value).ToString()
            : stat.value.ToString("0.##");

        return stat.valueType == ItemStatValueType.Percentage
            ? $"{valueText}%"
            : valueText;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon:
                return new Color(0.56f, 0.78f, 0.66f, 0.96f);
            case ItemRarity.Rare:
                return new Color(0.35f, 0.62f, 0.98f, 0.96f);
            case ItemRarity.Epic:
                return new Color(0.72f, 0.42f, 0.95f, 0.96f);
            case ItemRarity.Legendary:
                return new Color(0.96f, 0.62f, 0.24f, 0.96f);
            default:
                return new Color(0.68f, 0.82f, 0.76f, 0.96f);
        }
    }
}
