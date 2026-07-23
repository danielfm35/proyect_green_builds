using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrepareSlotUI : MonoBehaviour
{
    private static readonly List<PrepareSlotUI> RegisteredSlots = new();

    private static readonly Color ValidPreviewBorderColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    private static readonly Color InvalidPreviewBorderColor = new Color(1f, 0.18f, 0.12f, 1f);

    [SerializeField] private PrepareSlotKind slotKind;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image linkedItemBackgroundImage;
    [SerializeField] private Image linkedItemImage;
    [SerializeField] private RarityVisualConfig rarityVisualConfig;
    [SerializeField] private float borderThickness = 5f;

    private DraggedItemVisual linkedItem;
    private RectTransform borderRect;
    private Image borderTop;
    private Image borderRight;
    private Image borderBottom;
    private Image borderLeft;
    private bool sourceHighlightVisible;

    public PrepareSlotKind SlotKind => slotKind;
    public DraggedItemVisual LinkedItem => linkedItem;
    public bool HasLinkedItem => linkedItem != null;

    private void Awake()
    {
        RegisterSlot();
        EnsureBackgroundImage();
        EnsureRarityVisualConfig();
        EnsureLinkedItemBackgroundImage();
        EnsureLinkedItemImage();
        OrderLinkedVisuals();
        EnsureBorder();
        EnsureRarityVisualConfig();
        UpdateLinkedVisual();
        ClearPreview();
    }

    private void OnEnable()
    {
        RegisterSlot();
    }

    private void OnDisable()
    {
        SetSourceHighlightVisible(false);
    }

    private void OnDestroy()
    {
        SetSourceHighlightVisible(false);
        RegisteredSlots.Remove(this);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        EnsureBorder();
        UpdateLinkedVisual();
        ClearPreview();
    }

    private void LateUpdate()
    {
        if (linkedItem != null)
        {
            if (sourceHighlightVisible)
                SetSourceHighlight(linkedItem, true);

            return;
        }

        UpdateLinkedVisual();
        sourceHighlightVisible = false;
    }

    public void Configure(PrepareSlotKind kind)
    {
        slotKind = kind;
        EnsureBackgroundImage();
        EnsureRarityVisualConfig();
        EnsureLinkedItemBackgroundImage();
        EnsureLinkedItemImage();
        OrderLinkedVisuals();
        EnsureBorder();
        UpdateLinkedVisual();
        ClearPreview();
    }

    public bool CanAccept(DraggedItemVisual item)
    {
        ItemData data = item != null ? item.ItemData : null;
        if (data == null)
            return false;

        if (!CanAcceptBySlotKind(data))
            return false;

        PrepareSlotUI weaponSlot = GetRegisteredSlot(PrepareSlotKind.Weapon);
        PrepareSlotUI shieldSlot = GetRegisteredSlot(PrepareSlotKind.Shield);

        if (slotKind == PrepareSlotKind.Weapon && IsTwoHandedWeapon(data))
            return shieldSlot == null || shieldSlot.linkedItem == null || shieldSlot.linkedItem == item;

        if (slotKind == PrepareSlotKind.Shield && weaponSlot != null && weaponSlot.linkedItem != null && weaponSlot.linkedItem != item)
            return !IsTwoHandedWeapon(weaponSlot.linkedItem.ItemData);

        return true;
    }

    private bool CanAcceptBySlotKind(ItemData data)
    {
        switch (slotKind)
        {
            case PrepareSlotKind.Weapon:
                return data.itemType == ItemType.Weapon;
            case PrepareSlotKind.Shield:
                return data.itemType == ItemType.Shield || IsOneHandedWeapon(data);
            case PrepareSlotKind.Armor:
                return data.itemType == ItemType.Armor && data.armorCategory == ArmorCategory.Armor;
            case PrepareSlotKind.Boots:
                return data.itemType == ItemType.Armor && data.armorCategory == ArmorCategory.Boots;
            case PrepareSlotKind.Gloves:
                return data.itemType == ItemType.Armor && data.armorCategory == ArmorCategory.Gloves;
            case PrepareSlotKind.Helmet:
                return data.itemType == ItemType.Armor && data.armorCategory == ArmorCategory.Helmet;
            case PrepareSlotKind.Shoulder:
                return data.itemType == ItemType.Armor && data.armorCategory == ArmorCategory.Shoulder;
            default:
                return false;
        }
    }

    private static bool IsOneHandedWeapon(ItemData data)
    {
        return data != null && data.itemType == ItemType.Weapon && data.weaponHandedness == WeaponHandedness.OneHanded;
    }

    private static bool IsTwoHandedWeapon(ItemData data)
    {
        return data != null && data.itemType == ItemType.Weapon && data.weaponHandedness == WeaponHandedness.TwoHanded;
    }

    private static PrepareSlotUI GetRegisteredSlot(PrepareSlotKind kind)
    {
        for (int i = RegisteredSlots.Count - 1; i >= 0; i--)
        {
            PrepareSlotUI slot = RegisteredSlots[i];
            if (slot == null)
            {
                RegisteredSlots.RemoveAt(i);
                continue;
            }

            if (slot.slotKind == kind)
                return slot;
        }

        return null;
    }

    public bool TryLink(DraggedItemVisual item)
    {
        if (!CanAccept(item))
            return false;

        if (linkedItem != null && linkedItem != item)
            SetSourceHighlight(linkedItem, false);

        linkedItem = item;
        UpdateLinkedVisual();
        SetSourceHighlightVisible(true);
        return true;
    }

    public void ClearLink()
    {
        SetSourceHighlightVisible(false);
        linkedItem = null;
        UpdateLinkedVisual();
        ClearPreview();
    }

    public void ClearLinkIfLinkedTo(DraggedItemVisual item)
    {
        if (linkedItem != item)
            return;

        ClearLink();
    }

    public static void ClearDestroyedItem(DraggedItemVisual item)
    {
        for (int i = RegisteredSlots.Count - 1; i >= 0; i--)
        {
            PrepareSlotUI slot = RegisteredSlots[i];
            if (slot == null)
            {
                RegisteredSlots.RemoveAt(i);
                continue;
            }

            slot.ClearDestroyedLinkIfLinkedTo(item);
        }
    }

    private void ClearDestroyedLinkIfLinkedTo(DraggedItemVisual item)
    {
        if (linkedItem != item)
            return;

        linkedItem = null;
        sourceHighlightVisible = false;
    }

    public void ShowPreview(bool canAccept)
    {
        EnsureBorder();
        SetBorderColor(canAccept ? ValidPreviewBorderColor : InvalidPreviewBorderColor);
        SetBorderVisible(true);
    }

    public void ClearPreview()
    {
        SetBorderVisible(false);
    }

    public void SetSourceHighlightVisible(bool isVisible)
    {
        sourceHighlightVisible = isVisible;
        SetSourceHighlight(linkedItem, isVisible);
    }

    private void EnsureBackgroundImage()
    {
        if (backgroundImage != null)
            return;

        Transform backgroundTransform = transform.Find("background");
        if (backgroundTransform != null)
            backgroundImage = backgroundTransform.GetComponent<Image>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    private void RegisterSlot()
    {
        if (!RegisteredSlots.Contains(this))
            RegisteredSlots.Add(this);
    }

    private void EnsureLinkedItemImage()
    {
        if (linkedItemImage != null)
            return;

        Transform iconTransform = transform.Find("LinkedItemIcon");
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("LinkedItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            linkedItemImage = iconObject.GetComponent<Image>();
            linkedItemImage.raycastTarget = false;
            linkedItemImage.preserveAspect = true;
            linkedItemImage.enabled = false;
            OrderLinkedVisuals();
            return;
        }

        linkedItemImage = iconTransform.GetComponent<Image>();
        OrderLinkedVisuals();
    }

    private void EnsureLinkedItemBackgroundImage()
    {
        if (linkedItemBackgroundImage != null)
            return;

        Transform backgroundTransform = transform.Find("LinkedItemBackground");
        if (backgroundTransform == null)
        {
            GameObject backgroundObject = new GameObject("LinkedItemBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(transform, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.08f, 0.08f);
            backgroundRect.anchorMax = new Vector2(0.92f, 0.92f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            linkedItemBackgroundImage = backgroundObject.GetComponent<Image>();
            linkedItemBackgroundImage.raycastTarget = false;
            linkedItemBackgroundImage.preserveAspect = true;
            linkedItemBackgroundImage.enabled = false;
            OrderLinkedVisuals();
            return;
        }

        linkedItemBackgroundImage = backgroundTransform.GetComponent<Image>();
        OrderLinkedVisuals();
    }

    private void OrderLinkedVisuals()
    {
        if (linkedItemBackgroundImage != null)
            linkedItemBackgroundImage.transform.SetAsLastSibling();

        if (linkedItemImage != null)
            linkedItemImage.transform.SetAsLastSibling();

        if (borderRect != null)
            borderRect.transform.SetAsLastSibling();
    }

    private void EnsureBorder()
    {
        if (borderRect != null)
            return;

        Transform existingBorder = transform.Find("PrepareLinkedBorder");
        if (existingBorder != null)
        {
            borderRect = existingBorder.GetComponent<RectTransform>();
            borderTop = FindBorderLine("Top");
            borderRight = FindBorderLine("Right");
            borderBottom = FindBorderLine("Bottom");
            borderLeft = FindBorderLine("Left");
        }

        if (borderRect == null)
        {
            GameObject borderObject = new GameObject("PrepareLinkedBorder", typeof(RectTransform));
            borderObject.transform.SetParent(transform, false);
            borderObject.transform.SetAsLastSibling();
            borderRect = borderObject.GetComponent<RectTransform>();
        }

        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        if (borderTop == null)
            borderTop = CreateBorderLine("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, borderThickness));

        if (borderRight == null)
            borderRight = CreateBorderLine("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(borderThickness, 0f));

        if (borderBottom == null)
            borderBottom = CreateBorderLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, borderThickness));

        if (borderLeft == null)
            borderLeft = CreateBorderLine("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(borderThickness, 0f));
    }

    private Image FindBorderLine(string lineName)
    {
        if (borderRect == null)
            return null;

        Transform line = borderRect.Find(lineName);
        return line != null ? line.GetComponent<Image>() : null;
    }

    private Image CreateBorderLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(borderRect, false);

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

    private void UpdateLinkedVisual()
    {
        EnsureLinkedItemBackgroundImage();
        EnsureLinkedItemImage();
        if (linkedItemImage == null || linkedItemBackgroundImage == null)
            return;

        ItemData data = linkedItem != null ? linkedItem.ItemData : null;
        ItemInstance instance = linkedItem != null ? linkedItem.ItemInstance : null;

        if (data != null && instance != null)
            ApplyRarityVisual(instance.rarity);
        else
            ClearLinkedItemBackground();

        linkedItemImage.sprite = data != null ? data.icon : null;
        linkedItemImage.enabled = data != null && data.icon != null;
    }

    private void ApplyRarityVisual(ItemRarity rarity)
    {
        if (linkedItemBackgroundImage == null)
            return;

        EnsureRarityVisualConfig();

        linkedItemBackgroundImage.enabled = true;
        if (rarityVisualConfig != null)
        {
            rarityVisualConfig.ApplyTo(linkedItemBackgroundImage, rarity);
            return;
        }

        linkedItemBackgroundImage.sprite = null;
        linkedItemBackgroundImage.color = Color.white;
        linkedItemBackgroundImage.preserveAspect = false;
    }

    private void ClearLinkedItemBackground()
    {
        if (linkedItemBackgroundImage == null)
            return;

        linkedItemBackgroundImage.sprite = null;
        linkedItemBackgroundImage.enabled = false;
    }

    private void EnsureRarityVisualConfig()
    {
        if (rarityVisualConfig != null)
            return;

        RarityVisualConfig[] configs = Resources.LoadAll<RarityVisualConfig>(string.Empty);
        if (configs.Length > 0)
            rarityVisualConfig = configs[0];
    }

    private void SetBorderColor(Color color)
    {
        if (borderTop != null) borderTop.color = color;
        if (borderRight != null) borderRight.color = color;
        if (borderBottom != null) borderBottom.color = color;
        if (borderLeft != null) borderLeft.color = color;
    }

    private void SetBorderVisible(bool isVisible)
    {
        if (borderRect != null)
            borderRect.gameObject.SetActive(isVisible);
    }

    private void SetSourceHighlight(DraggedItemVisual item, bool isHighlighted)
    {
        if (item == null)
            return;

        if (item.CurrentAnvilSlot != null)
            item.CurrentAnvilSlot.SetPrepareLinkedHighlight(isHighlighted);

        if (item.CurrentChestSlot != null)
            item.CurrentChestSlot.SetPrepareLinkedHighlight(isHighlighted);
    }
}
