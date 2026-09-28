using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    private const int TooltipSortingOrder = 20000;
    private const float PanelWidth = 405f;
    private const float PanelHeight = 255f;
    private const float StatsPanelGap = 4f;
    private const float StatsPanelPaddingX = 24f;
    private const float StatsPanelPaddingY = 18f;
    private const float StatsRowHeight = 32f;
    private const float EnchantmentRowHeight = 82f;
    private const float AbilityRowHeight = 112f;

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
    private RectTransform enchantmentsPanel;
    private RectTransform abilitiesPanel;
    private RectTransform inventoryDetails;
    private Image inventoryIcon;
    private TextMeshProUGUI inventoryName;
    private TextMeshProUGUI inventoryBody;
    private Object tooltipOwner;

    public static void ShowEnchantment(EnchantmentInstance instance, Canvas canvas, Object owner)
    {
        if (instance?.data == null || canvas == null) return;
        EnsureTooltip(canvas);
        EnchantmentData data = instance.data;
        string group = data.requiredGroup.ToString();
        if (GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish)
        {
            string[] groups = { "Dagas", "Espadas", "Mazas", "Cascos", "Pesado", "Ligero", "Cuero" };
            int index = (int)data.requiredGroup;
            if (index >= 0 && index < groups.Length) group = groups[index];
        }
        string facts = $"{DetailLabel("Grupo requerido", "Required group")}: {group}\n" +
            $"{DetailLabel("Casillas afectadas", "Affected slots")}: {GetRuleName(instance.targetRule)}\n" +
            $"{DetailLabel("Multiplicador de daño por coincidencia", "Damage multiplier per match")}: +{data.damageMultiplierPerMatch:0.##}\n" +
            $"{DetailLabel("Máximo de coincidencias", "Maximum matches")}: {data.maximumMatches}";
        if (data.effectKind == EnchantmentData.EffectKind.DamageOnCraft)
            facts = $"{DetailLabel("Casillas afectadas", "Affected slots")}: {GetRuleName(instance.targetRule)}\n" +
                $"{DetailLabel("Daño por crafteo", "Damage per craft")}: +{data.damageIncreaseAmount:0.##}\n" +
                DetailLabel("Permanente. Solo objetos con daño. Subir rareza no lo activa.", "Permanent. Only items with damage. Rarity upgrades do not trigger it.");
        activeTooltip.SetInventoryDetails(data.enchantmentName, data.icon,
            DetailLabel("ENCANTAMIENTO", "ENCHANTMENT"), data.DisplayDescription, facts,
            DetailLabel("Arrastra sobre un objeto para encantarlo.", "Drag onto an item to enchant it."), owner);
    }

    public static void ShowModifier(ModifierInstance instance, Canvas canvas, Object owner)
    {
        if (instance?.data == null || canvas == null) return;
        EnsureTooltip(canvas);
        ModifierData data = instance.data;
        bool placement = data.property == ModifierData.ModifierProperty.PlacementRule;
        string side = data.crafterSide.ToString();
        if (GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish)
        {
            string[] sides = { "Arriba", "Derecha", "Abajo", "Izquierda" };
            int index = (int)data.crafterSide;
            if (index >= 0 && index < sides.Length) side = sides[index];
        }
        string facts = placement
            ? $"{DetailLabel("Modifica", "Changes")}: {DetailLabel("Restricción de ubicación", "Placement restriction")}\n{DetailLabel("Nueva ubicación", "New placement")}: {GetRuleName(data.placementRule)}"
            : $"{DetailLabel("Modifica", "Changes")}: {DetailLabel("Lado de conexión", "Crafter side")}\n{DetailLabel("Nuevo lado", "New side")}: {side}";
        activeTooltip.SetInventoryDetails(data.modifierName, data.icon,
            DetailLabel("MODIFICADOR", "MODIFIER"), data.description, facts,
            DetailLabel("Arrastra sobre un objeto para aplicar el cambio.", "Drag onto an item to apply the change."), owner);
    }

    private static string DetailLabel(string spanish, string english) =>
        GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish ? spanish : english;

    public static void HideFor(Object owner)
    {
        if (activeTooltip != null && activeTooltip.tooltipOwner == owner) Hide();
    }

    private void SetInventoryDetails(string name, Sprite icon, string category, string description,
        string facts, string hint, Object owner)
    {
        if (inventoryDetails == null)
        {
            GameObject panel = new GameObject("InventoryDetails", typeof(RectTransform));
            panel.layer = gameObject.layer;
            panel.transform.SetParent(transform, false);
            inventoryDetails = panel.GetComponent<RectTransform>();
            inventoryDetails.anchorMin = Vector2.zero;
            inventoryDetails.anchorMax = Vector2.one;
            inventoryDetails.offsetMin = inventoryDetails.offsetMax = Vector2.zero;
            inventoryIcon = CreateImage("InventoryIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(72f, -72f), new Vector2(96f, 96f));
            inventoryIcon.transform.SetParent(inventoryDetails, false);
            inventoryIcon.preserveAspect = true;
            inventoryIcon.raycastTarget = false;
            inventoryName = CreatePanelText(inventoryDetails, "InventoryName");
            inventoryName.rectTransform.anchoredPosition = new Vector2(138f, -24f);
            inventoryName.rectTransform.sizeDelta = new Vector2(243f, 96f);
            inventoryName.fontStyle = FontStyles.Bold;
            inventoryName.enableAutoSizing = true;
            inventoryName.fontSizeMin = 18f;
            inventoryName.fontSizeMax = 28f;
            inventoryBody = CreatePanelText(inventoryDetails, "InventoryBody");
            inventoryBody.rectTransform.anchoredPosition = new Vector2(24f, -138f);
            inventoryBody.fontSize = 20f;
            inventoryBody.textWrappingMode = TextWrappingModes.Normal;
            inventoryBody.overflowMode = TextOverflowModes.Overflow;
        }
        foreach (Transform child in transform) child.gameObject.SetActive(child == inventoryDetails);
        tooltipOwner = owner;
        inventoryIcon.sprite = icon;
        inventoryIcon.enabled = icon != null;
        inventoryName.text = name;
        inventoryBody.text = $"<color=#CAB0FF><b>{category}</b></color>\n\n" +
            (string.IsNullOrWhiteSpace(description) ? GameTextLocalizer.LocalizeNoDescription() : description) +
            $"\n\n{facts}\n\n<color=#9DAFC5>{hint}</color>";
        float height = inventoryBody.GetPreferredValues(inventoryBody.text, PanelWidth - 48f, Mathf.Infinity).y;
        inventoryBody.rectTransform.sizeDelta = new Vector2(PanelWidth - 48f, height);
        rectTransform.sizeDelta = new Vector2(PanelWidth, 162f + height);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public static void Show(ItemInstance itemInstance, Canvas canvas)
    {
        if (itemInstance == null || itemInstance.data == null || canvas == null)
        {
            Hide();
            return;
        }

        EnsureTooltip(canvas);
        activeTooltip.tooltipOwner = null;
        activeTooltip.rectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        foreach (Transform child in activeTooltip.transform)
            child.gameObject.SetActive(child != activeTooltip.inventoryDetails);
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
        Transform tooltipParent = EnsureTooltipLayer(canvas);
        if (tooltipParent == null)
            return;

        if (activeTooltip != null && activeTooltip.transform.parent == tooltipParent)
            return;

        if (activeTooltip != null)
            UnityEngine.Object.Destroy(activeTooltip.gameObject);

        GameObject tooltipObject = new GameObject("ItemTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ItemTooltipUI));
        tooltipObject.layer = tooltipParent.gameObject.layer;
        tooltipObject.transform.SetParent(tooltipParent, false);

        activeTooltip = tooltipObject.GetComponent<ItemTooltipUI>();
        activeTooltip.Build();
    }

    private static Transform EnsureTooltipLayer(Canvas sourceCanvas)
    {
        Canvas rootCanvas = sourceCanvas != null ? sourceCanvas.rootCanvas : null;
        if (rootCanvas == null)
            return null;

        Transform tooltipLayer = rootCanvas.transform.Find("TooltipLayer");
        if (tooltipLayer == null)
        {
            GameObject layerObject = new GameObject("TooltipLayer", typeof(RectTransform));
            layerObject.layer = rootCanvas.gameObject.layer;
            layerObject.transform.SetParent(rootCanvas.transform, false);
            tooltipLayer = layerObject.transform;

            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
        }

        Canvas tooltipCanvas = tooltipLayer.GetComponent<Canvas>();
        if (tooltipCanvas == null)
            tooltipCanvas = tooltipLayer.gameObject.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = TooltipSortingOrder;

        return tooltipLayer;
    }

    private void Build()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(40f, -30f);
        rectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image backgroundImage = GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 1f);
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
        UpdateStatsPanel(itemInstance.GetEffectiveStats());
        UpdateEnchantmentsPanel(itemInstance.enchantments);
        UpdateAbilitiesPanel(itemData.abilities);
    }

    private void UpdateAbilitiesPanel(List<AbilityData> abilities)
    {
        EnsureAbilitiesPanel();

        for (int i = abilitiesPanel.childCount - 1; i >= 0; i--)
            Destroy(abilitiesPanel.GetChild(i).gameObject);

        List<AbilityData> visibleAbilities = new();
        if (abilities != null)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] != null)
                    visibleAbilities.Add(abilities[i]);
            }
        }

        if (visibleAbilities.Count == 0)
        {
            abilitiesPanel.gameObject.SetActive(false);
            return;
        }

        float verticalOffset = StatsPanelGap;
        if (statsPanel != null && statsPanel.gameObject.activeSelf)
            verticalOffset += statsPanel.sizeDelta.y + StatsPanelGap;
        if (enchantmentsPanel != null && enchantmentsPanel.gameObject.activeSelf)
            verticalOffset += enchantmentsPanel.sizeDelta.y + StatsPanelGap;

        abilitiesPanel.anchoredPosition = new Vector2(0f, -verticalOffset);
        abilitiesPanel.sizeDelta = new Vector2(0f, 38f + visibleAbilities.Count * AbilityRowHeight);
        abilitiesPanel.gameObject.SetActive(true);

        TextMeshProUGUI heading = CreatePanelText(abilitiesPanel, "AbilitiesTitle");
        heading.text = GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish
            ? "HABILIDADES"
            : "ABILITIES";
        heading.fontSize = 18f;
        heading.fontStyle = FontStyles.Bold;
        heading.color = new Color(1f, 0.48f, 0.42f, 1f);
        heading.rectTransform.anchoredPosition = new Vector2(22f, -9f);
        heading.rectTransform.sizeDelta = new Vector2(PanelWidth - 44f, 25f);

        for (int i = 0; i < visibleAbilities.Count; i++)
        {
            AbilityData ability = visibleAbilities[i];
            float y = -38f - i * AbilityRowHeight;

            GameObject iconObject = new GameObject(
                "AbilityIcon_" + (i + 1),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            iconObject.transform.SetParent(abilitiesPanel, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(22f, y - 7f);
            iconRect.sizeDelta = new Vector2(76f, 76f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = ability.icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI name = CreatePanelText(abilitiesPanel, "AbilityName_" + (i + 1));
            name.text = ability.abilityName;
            name.fontSize = 18f;
            name.fontStyle = FontStyles.Bold;
            name.color = new Color(1f, 0.48f, 0.42f, 1f);
            name.rectTransform.anchoredPosition = new Vector2(112f, y - 2f);
            name.rectTransform.sizeDelta = new Vector2(PanelWidth - 134f, 26f);

            TextMeshProUGUI description = CreatePanelText(abilitiesPanel, "AbilityDescription_" + (i + 1));
            description.text = string.IsNullOrWhiteSpace(ability.description)
                ? GameTextLocalizer.LocalizeNoDescription()
                : ability.description;
            description.fontSize = 14f;
            description.rectTransform.anchoredPosition = new Vector2(112f, y - 30f);
            description.rectTransform.sizeDelta = new Vector2(PanelWidth - 134f, 50f);

            TextMeshProUGUI roll = CreatePanelText(abilitiesPanel, "AbilityRoll_" + (i + 1));
            string rollLabel = GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish
                ? "Activación"
                : "Activation";
            roll.text = $"<b>{rollLabel}: d20 ≥ {ability.minimumD20Roll}</b>";
            roll.fontSize = 15f;
            roll.color = new Color(1f, 0.82f, 0.28f, 1f);
            roll.rectTransform.anchoredPosition = new Vector2(112f, y - 82f);
            roll.rectTransform.sizeDelta = new Vector2(PanelWidth - 134f, 24f);
        }
    }

    private void EnsureAbilitiesPanel()
    {
        if (abilitiesPanel != null)
            return;

        GameObject panelObject = new GameObject(
            "AbilitiesTooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.transform.SetParent(transform, false);
        abilitiesPanel = panelObject.GetComponent<RectTransform>();
        abilitiesPanel.anchorMin = new Vector2(0f, 0f);
        abilitiesPanel.anchorMax = new Vector2(1f, 0f);
        abilitiesPanel.pivot = new Vector2(0.5f, 1f);
        abilitiesPanel.sizeDelta = Vector2.zero;
        Image background = panelObject.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;
        panelObject.SetActive(false);
    }

    private void UpdateEnchantmentsPanel(List<EnchantmentInstance> enchantments)
    {
        EnsureEnchantmentsPanel();

        for (int i = enchantmentsPanel.childCount - 1; i >= 0; i--)
            Destroy(enchantmentsPanel.GetChild(i).gameObject);

        int count = enchantments != null ? enchantments.Count : 0;
        if (count == 0)
        {
            enchantmentsPanel.gameObject.SetActive(false);
            return;
        }

        float statsHeight = statsPanel != null && statsPanel.gameObject.activeSelf
            ? statsPanel.sizeDelta.y + StatsPanelGap
            : 0f;
        enchantmentsPanel.anchoredPosition = new Vector2(0f, -StatsPanelGap - statsHeight);
        enchantmentsPanel.sizeDelta = new Vector2(0f, 38f + count * EnchantmentRowHeight);
        enchantmentsPanel.gameObject.SetActive(true);

        TextMeshProUGUI heading = CreatePanelText(enchantmentsPanel, "EnchantmentsTitle");
        heading.text = GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish
            ? "ENCANTAMIENTOS"
            : "ENCHANTMENTS";
        heading.fontSize = 18f;
        heading.color = new Color(0.96f, 0.96f, 0.86f, 1f);
        heading.rectTransform.anchoredPosition = new Vector2(22f, -9f);
        heading.rectTransform.sizeDelta = new Vector2(PanelWidth - 44f, 25f);

        for (int i = 0; i < count; i++)
        {
            EnchantmentInstance enchantment = enchantments[i];
            if (enchantment == null || enchantment.data == null)
                continue;

            float y = -38f - i * EnchantmentRowHeight;
            Color color = EnchantmentVisualPalette.GetColor(i);

            GameObject swatchObject = new GameObject("Color_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            swatchObject.transform.SetParent(enchantmentsPanel, false);
            RectTransform swatchRect = swatchObject.GetComponent<RectTransform>();
            swatchRect.anchorMin = swatchRect.anchorMax = new Vector2(0f, 1f);
            swatchRect.pivot = new Vector2(0f, 1f);
            swatchRect.anchoredPosition = new Vector2(14f, y - 5f);
            swatchRect.sizeDelta = new Vector2(8f, 62f);
            swatchObject.GetComponent<Image>().color = color;
            swatchObject.GetComponent<Image>().raycastTarget = false;

            GameObject iconObject = new GameObject("Icon_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(enchantmentsPanel, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(30f, y - 7f);
            iconRect.sizeDelta = new Vector2(54f, 54f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = enchantment.data.icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI details = CreatePanelText(enchantmentsPanel, "Details_" + (i + 1));
            string description = string.IsNullOrWhiteSpace(enchantment.data.description)
                ? GameTextLocalizer.LocalizeNoDescription()
                : enchantment.data.DisplayDescription;
            details.text = $"<b>{enchantment.data.enchantmentName}</b>\n{description}";
            details.rectTransform.anchoredPosition = new Vector2(96f, y);
            details.rectTransform.sizeDelta = new Vector2(PanelWidth - 112f, 52f);

            GameObject ruleIconObject = new GameObject(
                "RuleIcon_" + (i + 1),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            ruleIconObject.transform.SetParent(enchantmentsPanel, false);
            RectTransform ruleIconRect = ruleIconObject.GetComponent<RectTransform>();
            ruleIconRect.anchorMin = ruleIconRect.anchorMax = new Vector2(0f, 1f);
            ruleIconRect.pivot = new Vector2(0f, 1f);
            ruleIconRect.anchoredPosition = new Vector2(96f, y - 55f);
            ruleIconRect.sizeDelta = new Vector2(22f, 22f);
            Image ruleIcon = ruleIconObject.GetComponent<Image>();
            ruleIcon.sprite = GridPlacementRuleUtility.LoadRuleSprite(enchantment.targetRule);
            ruleIcon.color = color;
            ruleIcon.preserveAspect = true;
            ruleIcon.raycastTarget = false;

            TextMeshProUGUI location = CreatePanelText(enchantmentsPanel, "Location_" + (i + 1));
            string locationLabel = GameTextLocalizer.CurrentLanguage == GameLanguage.Spanish ? "Ubicación" : "Location";
            location.text = $"<b>{locationLabel}: {GetRuleName(enchantment.targetRule)}</b>";
            location.color = color;
            location.fontSize = 14f;
            location.rectTransform.anchoredPosition = new Vector2(124f, y - 55f);
            location.rectTransform.sizeDelta = new Vector2(PanelWidth - 140f, 24f);
        }
    }

    private void EnsureEnchantmentsPanel()
    {
        if (enchantmentsPanel != null)
            return;

        GameObject panelObject = new GameObject("EnchantmentsTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(transform, false);
        enchantmentsPanel = panelObject.GetComponent<RectTransform>();
        enchantmentsPanel.anchorMin = new Vector2(0f, 0f);
        enchantmentsPanel.anchorMax = new Vector2(1f, 0f);
        enchantmentsPanel.pivot = new Vector2(0.5f, 1f);
        enchantmentsPanel.sizeDelta = Vector2.zero;
        Image background = panelObject.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;
        panelObject.SetActive(false);
    }

    private static TextMeshProUGUI CreatePanelText(RectTransform parent, string name)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 15f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static string GetRuleName(GridPlacementRule rule)
    {
        if (GameTextLocalizer.CurrentLanguage != GameLanguage.Spanish)
            return rule.ToString();

        switch (rule)
        {
            case GridPlacementRule.Center: return "Centro";
            case GridPlacementRule.Corners: return "Esquinas";
            case GridPlacementRule.Up: return "Arriba";
            case GridPlacementRule.Down: return "Abajo";
            case GridPlacementRule.Left: return "Izquierda";
            case GridPlacementRule.Right: return "Derecha";
            case GridPlacementRule.LeftRight: return "Izquierda / Derecha";
            case GridPlacementRule.UpDown: return "Arriba / Abajo";
            case GridPlacementRule.Adjacent: return "Adyacente";
            default: return "Todo el yunque";
        }
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
        panelImage.color = Color.black;
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
