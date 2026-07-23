using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggedItemVisual : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image placementRuleIndicatorImage;
    [SerializeField] private Image protectionOverlayImage;
    [SerializeField] private Image burnOverlayImage;
    [SerializeField] private RarityVisualConfig rarityVisualConfig;

    [Header("Crafter Lines")]
    [SerializeField] private Image crafterLineTop;
    [SerializeField] private Image crafterLineRight;
    [SerializeField] private Image crafterLineBottom;
    [SerializeField] private Image crafterLineLeft;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private ItemInstance itemInstance;
    private ShopSlotUI originShopSlot;
    private Color defaultBackgroundColor = Color.white;
    private Sprite defaultBackgroundSprite;
    private bool defaultPreserveAspect;

    private Vector3 originalWorldPosition;
    private Transform originalParent;

    private AnvilSlotUI currentAnvilSlot;
    private AnvilSlotUI originAnvilSlot;
    private AnvilSlotUI previewedAnvilSlot;
    private PrepareSlotUI previewedPrepareSlot;

    private ChestSlotUI currentChestSlot;
    private ChestSlotUI originChestSlot;
    private SellZoneUI sellZone;
    private bool dragBlockedByBurn;
    private UIImpactScaleAnimation impactScaleAnimation;

    public ItemData ItemData => itemInstance != null ? itemInstance.data : null;
    public CrafterSide FusionSide => itemInstance != null ? itemInstance.crafterSide : CrafterSide.Top;
    public ItemInstance ItemInstance => itemInstance;
    public AnvilSlotUI CurrentAnvilSlot => currentAnvilSlot;
    public ChestSlotUI CurrentChestSlot => currentChestSlot;
    public bool IsBurned => itemInstance != null && itemInstance.isBurned;
    public float ImpactScaleAnimationDuration => EnsureImpactScaleAnimation().Duration;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInstance == null || itemInstance.data == null)
            return;

        Canvas hoverCanvas = canvas != null ? canvas : GetComponentInParent<Canvas>();
        canvas = hoverCanvas;
        ItemTooltipUI.Show(itemInstance, hoverCanvas);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Hide();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
            defaultBackgroundSprite = backgroundImage.sprite;
            defaultPreserveAspect = backgroundImage.preserveAspect;
        }

        EnsurePlacementRuleIndicator();
        EnsureProtectionVisuals();
        EnsureBurnVisuals();
        EnsureRarityVisualConfig();
    }

    private void OnDestroy()
    {
        ItemTooltipUI.Hide();
        PrepareSlotUI.ClearDestroyedItem(this);

        if (currentAnvilSlot != null)
            currentAnvilSlot.SetPrepareLinkedHighlight(false);

        if (currentChestSlot != null)
            currentChestSlot.SetPrepareLinkedHighlight(false);

        if (originAnvilSlot != null)
            originAnvilSlot.SetPrepareLinkedHighlight(false);

        if (originChestSlot != null)
            originChestSlot.SetPrepareLinkedHighlight(false);
    }

    public void Initialize(ItemInstance instance, ShopSlotUI shopSlot)
    {
        itemInstance = instance;
        originShopSlot = shopSlot;

        if (itemInstance == null || itemInstance.data == null)
        {
            ClearVisual();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = itemInstance.data.icon;
            iconImage.enabled = itemInstance.data.icon != null;
            iconImage.preserveAspect = true;
        }

        if (backgroundImage != null)
            ApplyRarityVisual(itemInstance.rarity);

        UpdateCrafterLineVisual(itemInstance.crafterSide);
        UpdatePlacementRuleVisual(itemInstance.placementRule);
        RefreshProtectionVisual();
        RefreshBurnVisual();
    }

    private void ClearVisual()
    {
        ItemTooltipUI.Hide();

        if (backgroundImage != null)
        {
            backgroundImage.sprite = defaultBackgroundSprite;
            backgroundImage.color = defaultBackgroundColor;
            backgroundImage.preserveAspect = defaultPreserveAspect;
        }

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        DisableAllCrafterLines();
        DisablePlacementRuleVisual();
        SetProtectionVisualActive(false);
        SetBurnVisualActive(false);
        ClearAnvilPlacementPreview();
    }

    private void UpdateCrafterLineVisual(CrafterSide side)
    {
        DisableAllCrafterLines();

        Image target = GetCrafterLineBySide(side);
        if (target != null)
            target.enabled = true;
    }

    private void DisableAllCrafterLines()
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

    private Image GetCrafterLineBySide(CrafterSide side)
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

    public void SetCurrentAnvilSlot(AnvilSlotUI slot)
    {
        currentAnvilSlot = slot;
        currentChestSlot = null;
    }

    public void SetCurrentChestSlot(ChestSlotUI slot)
    {
        currentChestSlot = slot;
        currentAnvilSlot = null;
    }

    public void SetRaycastBlocking(bool shouldBlock)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = shouldBlock;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return;

        ItemTooltipUI.Hide();

        dragBlockedByBurn = IsBurned;
        if (dragBlockedByBurn)
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            return;
        }

        originalWorldPosition = rectTransform.position;
        originalParent = transform.parent;

        originAnvilSlot = currentAnvilSlot;
        originChestSlot = currentChestSlot;

        canvasGroup.blocksRaycasts = false;
        ClearAnvilPlacementPreview();
        ClearPreparePlacementPreview();
        sellZone = SellZoneUI.GetOrCreate(canvas);
        if (sellZone != null)
            sellZone.Show(itemInstance);

        if (canvas != null)
        {
            Transform dragLayer = GetDragLayer();
            dragLayer.SetAsLastSibling();
            transform.SetParent(dragLayer, true);
            transform.SetAsLastSibling();
        }

        // Siempre que se arrastra, toma tamano del yunque.
        ApplyAnvilDragSize();

        if (originAnvilSlot != null)
            originAnvilSlot.ClearItem();

        if (originChestSlot != null)
            originChestSlot.ClearItem();

        if (currentAnvilSlot != null)
        {
            currentAnvilSlot.SetPrepareLinkedHighlight(false);
            currentAnvilSlot = null;
        }

        if (currentChestSlot != null)
        {
            currentChestSlot.SetPrepareLinkedHighlight(false);
            currentChestSlot = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return;

        if (dragBlockedByBurn)
            return;

        if (rectTransform == null)
            return;

        RectTransform dragAreaRect = rectTransform.parent as RectTransform;
        Camera conversionCamera = eventData.pressEventCamera;
        if (conversionCamera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            conversionCamera = canvas.worldCamera;

        if (dragAreaRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragAreaRect,
                eventData.position,
                conversionCamera,
                out Vector3 worldPoint))
        {
            rectTransform.position = worldPoint;
        }
        UpdatePlacementPreviewForPointer(eventData);
        if (sellZone != null)
            sellZone.UpdatePointer(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            ClearAnvilPlacementPreview();
            ClearPreparePlacementPreview();

            if (ReturnToOriginSlot())
                return;

            if (originalParent != null)
                transform.SetParent(originalParent, true);

            if (rectTransform != null)
                rectTransform.position = originalWorldPosition;

            return;
        }

        if (dragBlockedByBurn)
        {
            dragBlockedByBurn = false;
            return;
        }

        canvasGroup.blocksRaycasts = true;
        ClearAnvilPlacementPreview();
        ClearPreparePlacementPreview();

        if (sellZone != null && sellZone.ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
        {
            sellZone.Sell(this);
            return;
        }

        if (sellZone != null)
            sellZone.Hide();

        PrepareSlotUI prepareSlot = GetPrepareSlotUnderPointer(eventData);
        if (prepareSlot != null)
        {
            if (prepareSlot.CanAccept(this) && ReturnToOriginSlot())
            {
                prepareSlot.TryLink(this);
                return;
            }
        }

        AnvilSlotUI anvilSlot = GetAnvilSlotUnderPointer(eventData);
        if (anvilSlot != null)
        {
            if (!anvilSlot.IsOccupied && CanPlaceInAnvilSlot(anvilSlot))
            {
                PlaceInAnvilSlot(anvilSlot);
                return;
            }

            if (CanPlaceInAnvilSlot(anvilSlot) && TrySwapWithOccupiedAnvilSlot(anvilSlot))
                return;
        }

        ChestSlotUI chestSlot = GetChestSlotUnderPointer(eventData);
        if (chestSlot != null)
        {
            if (!chestSlot.IsOccupied)
            {
                PlaceInChestSlot(chestSlot);
                return;
            }

            if (TrySwapWithOccupiedChestSlot(chestSlot))
                return;
        }

        if (ReturnToOriginSlot())
            return;

        transform.SetParent(originalParent, true);
        rectTransform.position = originalWorldPosition;
    }

    private AnvilSlotUI GetAnvilSlotUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            AnvilSlotUI slot = result.gameObject.GetComponentInParent<AnvilSlotUI>();
            if (slot != null)
                return slot;
        }

        return null;
    }

    private ChestSlotUI GetChestSlotUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            ChestSlotUI slot = result.gameObject.GetComponentInParent<ChestSlotUI>();
            if (slot != null)
                return slot;
        }

        return null;
    }

    private PrepareSlotUI GetPrepareSlotUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            PrepareSlotUI slot = result.gameObject.GetComponentInParent<PrepareSlotUI>();
            if (slot != null)
                return slot;
        }

        return null;
    }

    private void PlaceInAnvilSlot(AnvilSlotUI slot)
    {
        SnapToAnvilSlot(slot, true);
    }

    public void SnapToAnvilSlot(AnvilSlotUI slot, bool triggerCraft)
    {
        if (slot == null)
            return;

        transform.SetParent(slot.transform, false);

        RectTransform slotRect = slot.GetComponent<RectTransform>();

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = slotRect.rect.size;

        canvasGroup.blocksRaycasts = true;
        ClearAnvilPlacementPreview();
        slot.SetItem(this);
        currentAnvilSlot = slot;
        currentChestSlot = null;

        if (triggerCraft)
        {
            AnvilCraftManager craftManager = AnvilCraftManager.GetOrCreate();
            craftManager.TryCraftFromSlot(slot);
        }
    }

    private void PlaceInChestSlot(ChestSlotUI slot)
    {
        SnapToChestSlot(slot);
    }

    public void SnapToChestSlot(ChestSlotUI slot)
    {
        if (slot == null)
            return;

        transform.SetParent(slot.transform, false);

        RectTransform slotRect = slot.GetComponent<RectTransform>();

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = slotRect.rect.size;

        canvasGroup.blocksRaycasts = true;
        ClearAnvilPlacementPreview();
        slot.SetItem(this);
        currentChestSlot = slot;
        currentAnvilSlot = null;
    }

    private void ApplyAnvilDragSize()
    {
        AnvilSlotUI anyAnvilSlot = FindFirstObjectByType<AnvilSlotUI>();
        if (anyAnvilSlot == null)
            return;

        RectTransform anvilRect = anyAnvilSlot.GetComponent<RectTransform>();
        if (anvilRect == null)
            return;

        rectTransform.sizeDelta = anvilRect.rect.size;
        rectTransform.localScale = Vector3.one;
    }

    public void ReplaceItemData(ItemData newData, ItemRarity newRarity, CrafterSide newSide)
    {
        if (itemInstance == null)
            return;

        itemInstance.data = newData;
        itemInstance.rarity = newRarity;
        itemInstance.crafterSide = newSide;

        if (iconImage != null)
        {
            iconImage.sprite = newData != null ? newData.icon : null;
            iconImage.enabled = newData != null && newData.icon != null;
            iconImage.preserveAspect = true;
        }

        if (backgroundImage != null && newData != null)
            ApplyRarityVisual(newRarity);

        UpdateCrafterLineVisual(newSide);
        UpdatePlacementRuleVisual(itemInstance.placementRule);
        RefreshProtectionVisual();
        RefreshBurnVisual();
    }

    private Transform GetDragLayer()
    {
        if (canvas == null)
            return transform.parent;

        Transform dragLayer = canvas.transform.Find("DragLayer");
        if (dragLayer == null)
            return canvas.transform;

        Canvas dragLayerCanvas = dragLayer.GetComponent<Canvas>();
        if (dragLayerCanvas == null)
            dragLayerCanvas = dragLayer.gameObject.AddComponent<Canvas>();

        dragLayerCanvas.overrideSorting = true;
        dragLayerCanvas.sortingOrder = short.MaxValue;
        return dragLayer;
    }

    public void PlayImpactScaleAnimation()
    {
        EnsureImpactScaleAnimation().Play();
    }

    public IEnumerator PlayImpactScaleAnimationAndWait()
    {
        yield return EnsureImpactScaleAnimation().PlayAndWait();
    }

    public void DestroyAfterImpactScaleAnimation()
    {
        StartCoroutine(DestroyAfterImpactScaleAnimationRoutine());
    }

    private IEnumerator DestroyAfterImpactScaleAnimationRoutine()
    {
        yield return PlayImpactScaleAnimationAndWait();
        Destroy(gameObject);
    }

    private UIImpactScaleAnimation EnsureImpactScaleAnimation()
    {
        if (impactScaleAnimation == null)
            impactScaleAnimation = GetComponent<UIImpactScaleAnimation>();

        if (impactScaleAnimation == null)
            impactScaleAnimation = gameObject.AddComponent<UIImpactScaleAnimation>();

        return impactScaleAnimation;
    }

    public void AddProtection()
    {
        if (itemInstance == null)
            return;

        itemInstance.isProtected = true;
        RefreshProtectionVisual();
    }

    public bool ConsumeProtection()
    {
        if (itemInstance == null || !itemInstance.isProtected)
            return false;

        itemInstance.isProtected = false;
        RefreshProtectionVisual();
        return true;
    }

    public void SetBurned(bool isBurned)
    {
        if (itemInstance == null)
            return;

        itemInstance.isBurned = isBurned;
        RefreshBurnVisual();
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

        backgroundImage.sprite = defaultBackgroundSprite;
        backgroundImage.color = defaultBackgroundColor;
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

    private bool TrySwapWithOccupiedAnvilSlot(AnvilSlotUI targetSlot)
    {
        DraggedItemVisual displacedItem = targetSlot.CurrentItem;
        if (displacedItem == null || displacedItem == this)
            return false;

        if (displacedItem.IsBurned)
            return false;

        if (originAnvilSlot != null)
        {
            displacedItem.SnapToAnvilSlot(originAnvilSlot, false);
            PlaceInAnvilSlot(targetSlot);
            return true;
        }

        if (originChestSlot != null)
        {
            displacedItem.SnapToChestSlot(originChestSlot);
            PlaceInAnvilSlot(targetSlot);
            return true;
        }

        return false;
    }

    private bool TrySwapWithOccupiedChestSlot(ChestSlotUI targetSlot)
    {
        DraggedItemVisual displacedItem = targetSlot.CurrentItem;
        if (displacedItem == null || displacedItem == this)
            return false;

        if (displacedItem.IsBurned)
            return false;

        if (originAnvilSlot != null)
        {
            displacedItem.SnapToAnvilSlot(originAnvilSlot, false);
            PlaceInChestSlot(targetSlot);
            return true;
        }

        if (originChestSlot != null)
        {
            displacedItem.SnapToChestSlot(originChestSlot);
            PlaceInChestSlot(targetSlot);
            return true;
        }

        return false;
    }

    private bool ReturnToOriginSlot()
    {
        if (originAnvilSlot != null)
        {
            SnapToAnvilSlot(originAnvilSlot, false);
            return true;
        }

        if (originChestSlot != null)
        {
            SnapToChestSlot(originChestSlot);
            return true;
        }

        return false;
    }

    private bool CanPlaceInAnvilSlot(AnvilSlotUI slot)
    {
        if (slot == null || itemInstance == null)
            return false;

        return AnvilCraftManager.GetOrCreate().CanPlaceItemInSlot(itemInstance, slot);
    }

    private void UpdatePlacementPreviewForPointer(PointerEventData eventData)
    {
        AnvilSlotUI anvilSlot = GetAnvilSlotUnderPointer(eventData);
        PrepareSlotUI prepareSlot = GetPrepareSlotUnderPointer(eventData);
        if (prepareSlot != null)
        {
            ClearAnvilPlacementPreview();
            if (previewedPrepareSlot != prepareSlot)
            {
                ClearPreparePlacementPreview();
                previewedPrepareSlot = prepareSlot;
            }

            previewedPrepareSlot.ShowPreview(prepareSlot.CanAccept(this));
            return;
        }

        ClearPreparePlacementPreview();

        if (anvilSlot == null)
        {
            ClearAnvilPlacementPreview();
            return;
        }

        if (previewedAnvilSlot != anvilSlot)
        {
            ClearAnvilPlacementPreview();
            previewedAnvilSlot = anvilSlot;
        }

        previewedAnvilSlot.ShowPlacementPreview(CanPlaceInAnvilSlot(anvilSlot));
    }

    private void EnsurePlacementRuleIndicator()
    {
        if (placementRuleIndicatorImage != null)
            return;

        GameObject indicatorObject = new GameObject("PlacementRuleIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicatorObject.transform.SetParent(transform, false);

        RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 0f);
        indicatorRect.pivot = new Vector2(1f, 0f);
        indicatorRect.anchoredPosition = new Vector2(-6f, 6f);
        indicatorRect.sizeDelta = new Vector2(24f, 24f);

        placementRuleIndicatorImage = indicatorObject.GetComponent<Image>();
        placementRuleIndicatorImage.enabled = false;
        placementRuleIndicatorImage.raycastTarget = false;
        placementRuleIndicatorImage.preserveAspect = true;
    }

    private void EnsureProtectionVisuals()
    {
        if (protectionOverlayImage == null)
        {
            GameObject overlayObject = new GameObject("ProtectionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            protectionOverlayImage = overlayObject.GetComponent<Image>();
            protectionOverlayImage.color = new Color(0.66f, 1f, 0.86f, 0.28f);
            protectionOverlayImage.raycastTarget = false;
        }

        SetProtectionVisualActive(false);
    }

    private void EnsureBurnVisuals()
    {
        if (burnOverlayImage == null)
        {
            GameObject overlayObject = new GameObject("BurnOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            burnOverlayImage = overlayObject.GetComponent<Image>();
            burnOverlayImage.color = new Color(1f, 0.34f, 0.08f, 0.58f);
            burnOverlayImage.raycastTarget = false;
            burnOverlayImage.preserveAspect = true;
        }

        if (burnOverlayImage.sprite == null)
            burnOverlayImage.sprite = LoadBurnSprite();

        SetBurnVisualActive(false);
    }

    private void RefreshProtectionVisual()
    {
        bool isProtected = itemInstance != null && itemInstance.isProtected;
        EnsureProtectionVisuals();
        SetProtectionVisualActive(isProtected);
    }

    private void SetProtectionVisualActive(bool isActive)
    {
        if (protectionOverlayImage != null)
            protectionOverlayImage.gameObject.SetActive(isActive);
    }

    private void RefreshBurnVisual()
    {
        bool isBurned = itemInstance != null && itemInstance.isBurned;
        EnsureBurnVisuals();
        SetBurnVisualActive(isBurned);
    }

    private void SetBurnVisualActive(bool isActive)
    {
        if (burnOverlayImage != null)
            burnOverlayImage.gameObject.SetActive(isActive);
    }

    private Sprite LoadBurnSprite()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/curses/burn");
        if (sprites == null || sprites.Length == 0)
            return null;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name == "burn_1")
                return sprites[i];
        }

        return sprites[0];
    }

    private void ClearAnvilPlacementPreview()
    {
        if (previewedAnvilSlot == null)
            return;

        previewedAnvilSlot.ClearPlacementPreview();
        previewedAnvilSlot = null;
    }

    private void ClearPreparePlacementPreview()
    {
        if (previewedPrepareSlot == null)
            return;

        previewedPrepareSlot.ClearPreview();
        previewedPrepareSlot = null;
    }
}
