using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(DraggableShopItem))]
public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image placementRuleIndicatorImage;
    [SerializeField] private Image priceIconImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private RarityVisualConfig rarityVisualConfig;

    [Header("Crafter Lines")]
    [SerializeField] private Image crafterLineTop;
    [SerializeField] private Image crafterLineRight;
    [SerializeField] private Image crafterLineBottom;
    [SerializeField] private Image crafterLineLeft;

    private ItemInstance currentItemInstance;
    private Color defaultEmptyColor = Color.white;
    private Sprite defaultEmptySprite;
    private bool defaultPreserveAspect;
    private Sprite goldSprite;

    private DraggableShopItem draggableItem;
    private Canvas parentCanvas;

    public ItemInstance CurrentItemInstance => currentItemInstance;
    public ItemData CurrentItem => currentItemInstance != null ? currentItemInstance.data : null;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItemInstance == null || currentItemInstance.data == null)
            return;

        ItemTooltipUI.Show(currentItemInstance, parentCanvas);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Hide();
    }

    private void Awake()
    {
        draggableItem = GetComponent<DraggableShopItem>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (backgroundImage != null)
        {
            defaultEmptyColor = backgroundImage.color;
            defaultEmptySprite = backgroundImage.sprite;
            defaultPreserveAspect = backgroundImage.preserveAspect;
        }

        EnsurePlacementRuleIndicator();
        EnsurePriceDisplay();
        EnsureRarityVisualConfig();
    }

    public void SetItem(ItemInstance itemInstance)
    {
        currentItemInstance = itemInstance;

        if (itemInstance == null || itemInstance.data == null)
        {
            ClearSlot();
            return;
        }

        ItemData itemData = itemInstance.data;

        if (iconImage != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.preserveAspect = true;
            iconImage.enabled = itemData.icon != null;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            ApplyRarityVisual(itemInstance.rarity);
        }

        UpdateCrafterLineVisual(itemInstance.crafterSide);
        UpdatePlacementRuleVisual(itemInstance.placementRule);
        UpdatePriceVisual(itemInstance);

        if (draggableItem != null)
        {
            draggableItem.Initialize(this, itemInstance, parentCanvas);
        }
    }

    public void HideItemVisual()
    {
        ItemTooltipUI.Hide();

        if (iconImage != null)
            iconImage.enabled = false;

        if (backgroundImage != null)
            backgroundImage.enabled = false;

        DisableAllFusionLines();
        DisablePlacementRuleVisual();
        DisablePriceVisual();
    }

    public void ShowItemVisual()
    {
        if (currentItemInstance == null || currentItemInstance.data == null)
            return;

        ItemData currentData = currentItemInstance.data;

        if (iconImage != null)
        {
            iconImage.enabled = currentData.icon != null;
            iconImage.sprite = currentData.icon;
            iconImage.preserveAspect = true;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            ApplyRarityVisual(currentItemInstance.rarity);
        }

        UpdateCrafterLineVisual(currentItemInstance.crafterSide);
        UpdatePlacementRuleVisual(currentItemInstance.placementRule);
        UpdatePriceVisual(currentItemInstance);
    }

    public void EmptySlot()
    {
        ItemTooltipUI.Hide();
        currentItemInstance = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.color = defaultEmptyColor;
            backgroundImage.sprite = defaultEmptySprite;
            backgroundImage.preserveAspect = defaultPreserveAspect;
        }

        DisableAllFusionLines();
        DisablePlacementRuleVisual();
        DisablePriceVisual();

        if (draggableItem != null)
        {
            draggableItem.Initialize(this, null, parentCanvas);
        }
    }

    public void ClearSlot()
    {
        ItemTooltipUI.Hide();
        currentItemInstance = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.color = defaultEmptyColor;
            backgroundImage.sprite = defaultEmptySprite;
            backgroundImage.preserveAspect = defaultPreserveAspect;
        }

        DisableAllFusionLines();
        DisablePlacementRuleVisual();
        DisablePriceVisual();

        if (draggableItem != null)
        {
            draggableItem.Initialize(this, null, parentCanvas);
        }
    }

    private void UpdateCrafterLineVisual(CrafterSide side)
    {
        DisableAllFusionLines();

        Image target = GetLineBySide(side);
        if (target != null)
            target.enabled = true;
    }

    private void DisableAllFusionLines()
    {
        if (crafterLineTop != null) crafterLineTop.enabled = false;
        if (crafterLineRight != null) crafterLineRight.enabled = false;
        if (crafterLineBottom != null) crafterLineBottom.enabled = false;
        if (crafterLineLeft != null) crafterLineLeft.enabled = false;
    }

    private void UpdatePlacementRuleVisual(GridPlacementRule rule)
    {
        EnsurePlacementRuleIndicator();
        DisablePlacementRuleVisual();

        if (placementRuleIndicatorImage == null || rule == GridPlacementRule.None)
            return;

        placementRuleIndicatorImage.sprite = GridPlacementRuleUtility.LoadRuleSprite(rule);
        placementRuleIndicatorImage.preserveAspect = true;
        placementRuleIndicatorImage.enabled = placementRuleIndicatorImage.sprite != null;
    }

    private void DisablePlacementRuleVisual()
    {
        if (placementRuleIndicatorImage != null)
            placementRuleIndicatorImage.enabled = false;
    }

    private void UpdatePriceVisual(ItemInstance itemInstance)
    {
        EnsurePriceDisplay();

        if (itemInstance == null || itemInstance.data == null)
        {
            DisablePriceVisual();
            return;
        }

        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        int cost = shopManager != null
            ? shopManager.GetCurrentItemCost(itemInstance.rarity)
            : ShopManager.GetGoldCost(itemInstance.rarity);

        if (priceIconImage != null)
            priceIconImage.enabled = cost > 0 && priceIconImage.sprite != null;

        if (priceText != null)
        {
            priceText.enabled = true;
            priceText.text = cost > 0 ? cost.ToString() : "GRATIS";

            RectTransform priceRect = priceText.rectTransform;
            priceRect.anchoredPosition = cost > 0
                ? new Vector2(36f, 5f)
                : new Vector2(8f, 5f);
            priceRect.sizeDelta = cost > 0
                ? new Vector2(42f, 32f)
                : new Vector2(82f, 32f);
            priceText.alignment = cost > 0
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Center;
        }
    }

    public void RefreshPriceVisual()
    {
        if (currentItemInstance != null)
            UpdatePriceVisual(currentItemInstance);
    }

    private void DisablePriceVisual()
    {
        if (priceIconImage != null)
            priceIconImage.enabled = false;

        if (priceText != null)
            priceText.enabled = false;
    }

    private Image GetLineBySide(CrafterSide side)
    {
        switch (side)
        {
            case CrafterSide.Top: return crafterLineTop;
            case CrafterSide.Right: return crafterLineRight;
            case CrafterSide.Bottom: return crafterLineBottom;
            case CrafterSide.Left: return crafterLineLeft;
            default: return null;
        }
    }

    private void ApplyRarityVisual(ItemRarity rarity)
    {
        if (backgroundImage == null)
            return;

        EnsureRarityVisualConfig();

        if (rarityVisualConfig != null)
        {
            rarityVisualConfig.ApplyTo(backgroundImage, rarity);
            return;
        }

        backgroundImage.sprite = defaultEmptySprite;
        backgroundImage.color = defaultEmptyColor;
        backgroundImage.preserveAspect = defaultPreserveAspect;
    }

    private void EnsureRarityVisualConfig()
    {
        if (rarityVisualConfig != null)
            return;

        RarityVisualConfig[] configs = Resources.LoadAll<RarityVisualConfig>(string.Empty);
        if (configs.Length > 0)
            rarityVisualConfig = configs[0];
    }

    private void EnsurePlacementRuleIndicator()
    {
        if (placementRuleIndicatorImage != null)
            return;

        GameObject indicatorObject = new GameObject("PlacementRuleIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicatorObject.transform.SetParent(transform, false);

        RectTransform rectTransform = indicatorObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = new Vector2(-6f, 6f);
        rectTransform.sizeDelta = new Vector2(24f, 24f);

        placementRuleIndicatorImage = indicatorObject.GetComponent<Image>();
        placementRuleIndicatorImage.enabled = false;
        placementRuleIndicatorImage.raycastTarget = false;
        placementRuleIndicatorImage.preserveAspect = true;
    }

    private void EnsurePriceDisplay()
    {
        if (goldSprite == null)
            goldSprite = Resources.Load<Sprite>("Images/UI/gold");

        if (priceIconImage == null)
        {
            GameObject iconObject = new GameObject("PriceGoldIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 0f);
            iconRect.pivot = new Vector2(0f, 0f);
            iconRect.anchoredPosition = new Vector2(8f, 8f);
            iconRect.sizeDelta = new Vector2(28f, 28f);

            priceIconImage = iconObject.GetComponent<Image>();
            priceIconImage.raycastTarget = false;
            priceIconImage.preserveAspect = true;
            priceIconImage.sprite = goldSprite;
            priceIconImage.enabled = false;
        }

        if (priceText == null)
        {
            GameObject textObject = new GameObject("PriceText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(0f, 0f);
            textRect.pivot = new Vector2(0f, 0f);
            textRect.anchoredPosition = new Vector2(36f, 5f);
            textRect.sizeDelta = new Vector2(42f, 32f);

            priceText = textObject.GetComponent<TextMeshProUGUI>();
            priceText.raycastTarget = false;
            priceText.fontSize = 24f;
            priceText.fontStyle = FontStyles.Bold;
            priceText.alignment = TextAlignmentOptions.Left;
            priceText.color = new Color(1f, 0.84f, 0.21f, 1f);
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = 16f;
            priceText.fontSizeMax = 24f;
            priceText.enabled = false;
        }
    }
}
