using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ChestSlotCategory
{
    Item,
    Enchantment,
    Modifier,
}

public class ChestSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (currentEnchantment != null) ItemTooltipUI.ShowEnchantment(currentEnchantment, canvas, this);
        else if (currentModifier != null) ItemTooltipUI.ShowModifier(currentModifier, canvas, this);
    }

    public void OnPointerExit(PointerEventData eventData) => ItemTooltipUI.HideFor(this);
    private void OnDisable() => ItemTooltipUI.HideFor(this);
    private static readonly Color PrepareLinkedBorderColor = new Color(1f, 0.82f, 0.08f, 1f);

    private DraggedItemVisual currentItem;
    private EnchantmentInstance currentEnchantment;
    private ModifierInstance currentModifier;
    private RectTransform prepareLinkedBorder;
    private Image prepareLinkedBorderTop;
    private Image prepareLinkedBorderRight;
    private Image prepareLinkedBorderBottom;
    private Image prepareLinkedBorderLeft;

    [SerializeField] private ChestSlotCategory category = ChestSlotCategory.Item;

    public bool IsOccupied => currentItem != null || currentEnchantment != null || currentModifier != null;
    public DraggedItemVisual CurrentItem => currentItem;
    public EnchantmentInstance CurrentEnchantment => currentEnchantment;
    public ModifierInstance CurrentModifier => currentModifier;
    public ChestSlotCategory Category => category;
    public bool AcceptsItems => category == ChestSlotCategory.Item;

    public void Configure(ChestSlotCategory newCategory)
    {
        category = newCategory;
    }

    public bool CanAcceptItem()
    {
        return AcceptsItems && !IsOccupied;
    }

    public bool CanAcceptEnchantment()
    {
        return category == ChestSlotCategory.Enchantment && !IsOccupied;
    }

    public bool CanAcceptModifier()
    {
        return category == ChestSlotCategory.Modifier && !IsOccupied;
    }

    public bool TrySetModifier(ModifierInstance modifier)
    {
        if (modifier == null || modifier.data == null || !CanAcceptModifier())
            return false;

        currentModifier = modifier;
        GameObject iconObject = new GameObject(
            "ModifierIcon_" + modifier.data.name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(ModifierDragVisual));
        iconObject.layer = gameObject.layer;
        iconObject.transform.SetParent(transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(7f, 7f);
        iconRect.offsetMax = new Vector2(-7f, -7f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = modifier.data.icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = true;
        iconObject.GetComponent<ModifierDragVisual>().Initialize(modifier, this);
        return true;
    }

    public bool RemoveModifier(ModifierInstance modifier)
    {
        if (currentModifier == null || currentModifier != modifier)
            return false;

        currentModifier = null;
        ItemTooltipUI.HideFor(this);
        return true;
    }

    public bool TrySetEnchantment(EnchantmentInstance enchantment)
    {
        if (enchantment == null || enchantment.data == null || !CanAcceptEnchantment())
            return false;

        currentEnchantment = enchantment;

        GameObject iconObject = new GameObject(
            "EnchantmentIcon_" + enchantment.data.name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(EnchantmentDragVisual));
        iconObject.layer = gameObject.layer;
        iconObject.transform.SetParent(transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = enchantment.data.icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = true;
        iconObject.GetComponent<EnchantmentDragVisual>().Initialize(enchantment, this);

        Sprite ruleSprite = GridPlacementRuleUtility.LoadRuleSprite(enchantment.targetRule);
        if (ruleSprite != null)
        {
            GameObject ruleObject = new GameObject(
                "TargetRule_" + enchantment.targetRule,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            ruleObject.layer = gameObject.layer;
            ruleObject.transform.SetParent(iconObject.transform, false);

            RectTransform ruleRect = ruleObject.GetComponent<RectTransform>();
            ruleRect.anchorMin = ruleRect.anchorMax = new Vector2(1f, 0f);
            ruleRect.pivot = new Vector2(1f, 0f);
            ruleRect.anchoredPosition = new Vector2(-1f, 1f);
            ruleRect.sizeDelta = new Vector2(20f, 20f);

            Image ruleImage = ruleObject.GetComponent<Image>();
            ruleImage.sprite = ruleSprite;
            ruleImage.preserveAspect = true;
            ruleImage.raycastTarget = false;
        }
        return true;
    }

    public bool RemoveEnchantment(EnchantmentInstance enchantment)
    {
        if (currentEnchantment == null || currentEnchantment != enchantment)
            return false;

        currentEnchantment = null;
        ItemTooltipUI.HideFor(this);
        return true;
    }

    public void SetItem(DraggedItemVisual item)
    {
        currentItem = item;
    }

    public void ClearItem()
    {
        currentItem = null;
    }

    public void SetPrepareLinkedHighlight(bool isHighlighted)
    {
        if (!isHighlighted && prepareLinkedBorder == null)
            return;

        EnsurePrepareLinkedBorder();
        if (prepareLinkedBorder != null)
            prepareLinkedBorder.gameObject.SetActive(isHighlighted);
    }

    private void EnsurePrepareLinkedBorder()
    {
        if (prepareLinkedBorder != null)
            return;

        GameObject borderObject = new GameObject("PrepareLinkedBorder", typeof(RectTransform));
        borderObject.transform.SetParent(transform, false);
        borderObject.transform.SetAsLastSibling();

        prepareLinkedBorder = borderObject.GetComponent<RectTransform>();
        prepareLinkedBorder.anchorMin = Vector2.zero;
        prepareLinkedBorder.anchorMax = Vector2.one;
        prepareLinkedBorder.offsetMin = Vector2.zero;
        prepareLinkedBorder.offsetMax = Vector2.zero;

        prepareLinkedBorderTop = CreatePrepareLinkedBorderLine("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 5f));
        prepareLinkedBorderRight = CreatePrepareLinkedBorderLine("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(5f, 0f));
        prepareLinkedBorderBottom = CreatePrepareLinkedBorderLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f));
        prepareLinkedBorderLeft = CreatePrepareLinkedBorderLine("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(5f, 0f));

        SetPrepareLinkedBorderColor(PrepareLinkedBorderColor);
        prepareLinkedBorder.gameObject.SetActive(false);
    }

    private Image CreatePrepareLinkedBorderLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(prepareLinkedBorder, false);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        lineRect.sizeDelta = sizeDelta;

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.raycastTarget = false;
        return lineImage;
    }

    private void SetPrepareLinkedBorderColor(Color color)
    {
        if (prepareLinkedBorderTop != null) prepareLinkedBorderTop.color = color;
        if (prepareLinkedBorderRight != null) prepareLinkedBorderRight.color = color;
        if (prepareLinkedBorderBottom != null) prepareLinkedBorderBottom.color = color;
        if (prepareLinkedBorderLeft != null) prepareLinkedBorderLeft.color = color;
    }
}
