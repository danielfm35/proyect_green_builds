using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CanvasGroup))]
public class DraggableShopItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Dragged Visual Prefab")]
    [SerializeField] private DraggedItemVisual draggedVisualPrefab;
    [SerializeField] private bool enableDebugLogs = true;

    private CanvasGroup canvasGroup;

    private ShopSlotUI shopSlot;
    private ItemInstance itemInstance;
    private Canvas parentCanvas;
    private ShopManager shopManager;

    private DraggedItemVisual activeDraggedVisual;
    private RectTransform activeDraggedRect;

    private bool isInitialized;
    private bool isHoldingDisplacedItem;
    private int carryActivationFrame = -1;
    private AnvilSlotUI carryOriginAnvilSlot;
    private ChestSlotUI carryOriginChestSlot;
    private AnvilSlotUI previewedAnvilSlot;

    private static DraggableShopItem carryOwner;

    public void Initialize(ShopSlotUI slot, ItemInstance instance, Canvas canvas)
    {
        shopSlot = slot;
        itemInstance = instance;
        parentCanvas = canvas;
        isInitialized = true;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        shopManager = FindFirstObjectByType<ShopManager>();
    }

    private void Update()
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return;

        if (!isHoldingDisplacedItem || activeDraggedRect == null || parentCanvas == null)
            return;

        Vector2? pointerPosition = GetPointerScreenPosition();
        if (!pointerPosition.HasValue)
            return;

        UpdateDraggedPosition(pointerPosition.Value, null);

        if (Time.frameCount == carryActivationFrame)
            return;

        if (WasPrimaryPointerReleasedThisFrame())
            TryResolveCarryDrop();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return;

        if (carryOwner != null && carryOwner != this && carryOwner.isHoldingDisplacedItem)
        {
            LogDebug("OnBeginDrag bloqueado: ya existe otro item desplazado en la mano.");
            return;
        }

        if (!isInitialized || itemInstance == null || itemInstance.data == null || parentCanvas == null || draggedVisualPrefab == null)
        {
            LogDebug("OnBeginDrag cancelado por falta de inicializacion, item, canvas o prefab.");
            return;
        }

        LogDebug($"Comenzando drag de {DescribeItem()}.");

        Transform dragLayer = GetDragLayer();
        activeDraggedVisual = Instantiate(draggedVisualPrefab, dragLayer);
        activeDraggedRect = activeDraggedVisual.GetComponent<RectTransform>();

        dragLayer.SetAsLastSibling();
        activeDraggedVisual.transform.SetAsLastSibling();

        activeDraggedVisual.Initialize(itemInstance, shopSlot);
        activeDraggedVisual.SetRaycastBlocking(false);

        ApplyAnvilDragSize();

        activeDraggedRect.anchorMin = new Vector2(0.5f, 0.5f);
        activeDraggedRect.anchorMax = new Vector2(0.5f, 0.5f);
        activeDraggedRect.pivot = new Vector2(0.5f, 0.5f);
        activeDraggedRect.localScale = Vector3.one;

        UpdateDraggedPosition(eventData.position, eventData.pressEventCamera);

        shopSlot.HideItemVisual();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return;

        if (activeDraggedRect == null || parentCanvas == null || isHoldingDisplacedItem)
            return;

        UpdateDraggedPosition(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (AnvilCraftManager.IsResolvingCraftSequence)
        {
            if (activeDraggedVisual != null)
                CancelDrag();

            return;
        }

        if (activeDraggedVisual == null)
        {
            LogDebug("OnEndDrag cancelado: no existe activeDraggedVisual.");
            return;
        }

        AnvilSlotUI anvilSlot = GetAnvilSlotUnderPointer(eventData);
        if (anvilSlot != null)
        {
            if (!anvilSlot.IsOccupied && CanPlaceInAnvilSlot(anvilSlot))
            {
                LogDebug($"Soltado sobre slot de yunque valido ({anvilSlot.Row}, {anvilSlot.Column}).");
                PlaceInAnvilSlot(anvilSlot);
                return;
            }

            if (anvilSlot.IsOccupied && CanPlaceInAnvilSlot(anvilSlot))
            {
                LogDebug($"Swap desde tienda sobre slot de yunque ocupado ({anvilSlot.Row}, {anvilSlot.Column}).");
                SwapIntoOccupiedAnvilSlot(anvilSlot, eventData);
                return;
            }

            LogDebug($"Drop invalido por regla de posicion en slot ({anvilSlot.Row}, {anvilSlot.Column}).");
            CancelDrag();
            return;
        }

        ChestSlotUI chestSlot = GetChestSlotUnderPointer(eventData);
        if (chestSlot != null)
        {
            if (chestSlot.CanAcceptItem())
            {
                LogDebug("Soltado sobre slot de cofre valido.");
                PlaceInChestSlot(chestSlot);
                return;
            }

            if (chestSlot.AcceptsItems)
            {
                LogDebug("Swap desde tienda sobre slot de cofre ocupado.");
                SwapIntoOccupiedChestSlot(chestSlot, eventData);
                return;
            }

            CancelDrag();
            return;
        }

        LogDebug("Drop invalido, se cancela el drag y vuelve al origen.");
        CancelDrag();
    }

    private void ApplyAnvilDragSize()
    {
        AnvilSlotUI anyAnvilSlot = FindFirstObjectByType<AnvilSlotUI>();
        if (anyAnvilSlot != null)
        {
            RectTransform anvilRect = anyAnvilSlot.GetComponent<RectTransform>();
            activeDraggedRect.sizeDelta = anvilRect.rect.size;
        }
    }

    private void UpdateDraggedPosition(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform dragAreaRect = activeDraggedRect.parent as RectTransform;
        if (dragAreaRect == null)
            dragAreaRect = parentCanvas.GetComponent<RectTransform>();

        Camera conversionCamera = eventCamera;
        if (conversionCamera == null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            conversionCamera = parentCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragAreaRect,
            screenPosition,
            conversionCamera,
            out Vector2 localPoint
        );

        activeDraggedRect.anchoredPosition = localPoint;
        UpdatePlacementPreview(screenPosition);
    }

    private Transform GetDragLayer()
    {
        Transform dragLayer = parentCanvas.transform.Find("DragLayer");
        if (dragLayer == null)
            return parentCanvas.transform;

        Canvas dragLayerCanvas = dragLayer.GetComponent<Canvas>();
        if (dragLayerCanvas == null)
            dragLayerCanvas = dragLayer.gameObject.AddComponent<Canvas>();

        dragLayerCanvas.overrideSorting = true;
        dragLayerCanvas.sortingOrder = short.MaxValue;
        return dragLayer;
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

    private void PlaceInAnvilSlot(AnvilSlotUI slot)
    {
        if (!TryPayForCurrentItem())
            return;

        LogDebug($"Colocando {DescribeItem()} en slot ({slot.Row}, {slot.Column}).");

        activeDraggedVisual.SnapToAnvilSlot(slot, true);
        shopSlot.EmptySlot();
        ClearActiveDraggedState();
    }

    private void PlaceInChestSlot(ChestSlotUI slot)
    {
        if (slot == null || !slot.CanAcceptItem())
        {
            CancelDrag();
            return;
        }

        if (!TryPayForCurrentItem())
            return;

        LogDebug($"Colocando {DescribeItem()} en slot de cofre.");

        activeDraggedVisual.SnapToChestSlot(slot);
        shopSlot.EmptySlot();
        ClearActiveDraggedState();
    }

    private void SwapIntoOccupiedAnvilSlot(AnvilSlotUI slot, PointerEventData eventData)
    {
        if (!TryPayForCurrentItem())
            return;

        DraggedItemVisual displacedItem = slot.CurrentItem;
        if (displacedItem == null)
        {
            LogDebug($"Colocando {DescribeItem()} en slot ({slot.Row}, {slot.Column}).");
            activeDraggedVisual.SnapToAnvilSlot(slot, true);
            shopSlot.EmptySlot();
            ClearActiveDraggedState();
            return;
        }

        activeDraggedVisual.SnapToAnvilSlot(slot, true);
        shopSlot.EmptySlot();

        if (!IsDraggedVisualAlive(activeDraggedVisual) || !IsDraggedVisualAlive(displacedItem))
        {
            ClearActiveDraggedState();
            return;
        }

        StartHoldingDraggedVisual(displacedItem, eventData);
    }

    private void SwapIntoOccupiedChestSlot(ChestSlotUI slot, PointerEventData eventData)
    {
        if (slot == null || !slot.AcceptsItems)
        {
            CancelDrag();
            return;
        }

        if (!TryPayForCurrentItem())
            return;

        DraggedItemVisual displacedItem = slot.CurrentItem;
        if (displacedItem == null)
        {
            LogDebug($"Colocando {DescribeItem()} en slot de cofre.");
            activeDraggedVisual.SnapToChestSlot(slot);
            shopSlot.EmptySlot();
            ClearActiveDraggedState();
            return;
        }

        activeDraggedVisual.SnapToChestSlot(slot);
        shopSlot.EmptySlot();
        StartHoldingDraggedVisual(displacedItem, eventData);
    }

    private void StartHoldingDraggedVisual(DraggedItemVisual displacedItem, PointerEventData eventData)
    {
        if (displacedItem == null)
        {
            LogDebug("StartHoldingDraggedVisual cancelado: displacedItem es null.");
            ClearActiveDraggedState();
            return;
        }

        activeDraggedVisual = displacedItem;
        activeDraggedRect = displacedItem.GetComponent<RectTransform>();
        carryOriginAnvilSlot = displacedItem.CurrentAnvilSlot;
        carryOriginChestSlot = displacedItem.CurrentChestSlot;

        Transform dragLayer = GetDragLayer();
        dragLayer.SetAsLastSibling();
        displacedItem.transform.SetParent(dragLayer, true);
        displacedItem.transform.SetAsLastSibling();
        displacedItem.SetCurrentAnvilSlot(null);
        displacedItem.SetCurrentChestSlot(null);
        displacedItem.SetRaycastBlocking(false);

        ApplyAnvilDragSize();
        UpdateDraggedPosition(eventData.position, eventData.pressEventCamera);

        isHoldingDisplacedItem = true;
        carryOwner = this;
        carryActivationFrame = Time.frameCount;

        LogDebug(
            $"Entrando en carry con {DescribeDraggedVisual(activeDraggedVisual)}. " +
            $"frame={carryActivationFrame}, pointer={eventData.position}."
        );
    }

    private void TryResolveCarryDrop()
    {
        Vector2? pointerPosition = GetPointerScreenPosition();
        if (!pointerPosition.HasValue)
        {
            LogDebug("TryResolveCarryDrop cancelado: no hay pointerPosition.");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition.Value
        };

        LogDebug(
            $"TryResolveCarryDrop evaluando {DescribeDraggedVisual(activeDraggedVisual)} " +
            $"en pointer={pointerPosition.Value}."
        );

        AnvilSlotUI anvilSlot = GetAnvilSlotUnderPointer(pointerEventData);
        if (anvilSlot != null)
        {
            LogDebug(
                $"Carry detecto slot yunque ({anvilSlot.Row}, {anvilSlot.Column}), " +
                $"ocupado={anvilSlot.IsOccupied}, permitido={CanPlaceCarryInAnvilSlot(anvilSlot)}."
            );

            if (!anvilSlot.IsOccupied && CanPlaceCarryInAnvilSlot(anvilSlot))
            {
                activeDraggedVisual.SnapToAnvilSlot(anvilSlot, true);
                ClearActiveDraggedState();
                return;
            }

            DraggedItemVisual nextDisplaced = anvilSlot.CurrentItem;
            if (CanPlaceCarryInAnvilSlot(anvilSlot) && nextDisplaced != null && nextDisplaced != activeDraggedVisual)
            {
                LogDebug($"Carry hara swap con {DescribeDraggedVisual(nextDisplaced)}.");
                activeDraggedVisual.SnapToAnvilSlot(anvilSlot, true);

                if (!IsDraggedVisualAlive(activeDraggedVisual) || !IsDraggedVisualAlive(nextDisplaced))
                {
                    LogDebug("Carry cleanup: activeDraggedVisual o nextDisplaced ya no existe despues del snap.");
                    ClearActiveDraggedState();
                    return;
                }

                StartHoldingDraggedVisual(nextDisplaced, pointerEventData);
                return;
            }

            LogDebug("Carry sobre yunque no pudo resolverse.");
            return;
        }

        ChestSlotUI chestSlot = GetChestSlotUnderPointer(pointerEventData);
        if (chestSlot != null)
        {
            LogDebug($"Carry detecto slot cofre, ocupado={chestSlot.IsOccupied}.");

            if (chestSlot.CanAcceptItem())
            {
                activeDraggedVisual.SnapToChestSlot(chestSlot);
                ClearActiveDraggedState();
                return;
            }

            if (!chestSlot.AcceptsItems)
                return;

            DraggedItemVisual nextDisplaced = chestSlot.CurrentItem;
            if (nextDisplaced != null && nextDisplaced != activeDraggedVisual)
            {
                LogDebug($"Carry hara swap de cofre con {DescribeDraggedVisual(nextDisplaced)}.");
                activeDraggedVisual.SnapToChestSlot(chestSlot);
                StartHoldingDraggedVisual(nextDisplaced, pointerEventData);
                return;
            }

            LogDebug("Carry sobre cofre no pudo resolverse.");
            return;
        }

        LogDebug("Carry no encontro destino valido este frame.");
    }

    private void ClearActiveDraggedState()
    {
        if (activeDraggedVisual != null)
            activeDraggedVisual.SetRaycastBlocking(true);

        ClearAnvilPlacementPreview();

        activeDraggedVisual = null;
        activeDraggedRect = null;
        isHoldingDisplacedItem = false;
        if (carryOwner == this)
            carryOwner = null;
        carryActivationFrame = -1;
        carryOriginAnvilSlot = null;
        carryOriginChestSlot = null;
    }

    private void CancelDrag()
    {
        LogDebug($"Cancelando drag de {DescribeItem()}.");
        DraggedItemVisual visualToDestroy = activeDraggedVisual;
        ClearActiveDraggedState();
        if (visualToDestroy != null)
            Destroy(visualToDestroy.gameObject);
        shopSlot.ShowItemVisual();
    }

    private bool TryPayForCurrentItem()
    {
        if (itemInstance == null)
        {
            CancelDrag();
            return false;
        }

        if (shopManager == null)
            shopManager = FindFirstObjectByType<ShopManager>();

        if (shopManager == null)
        {
            LogDebug("Compra cancelada: no se encontro ShopManager para descontar oro.");
            CancelDrag();
            return false;
        }

        int cost = shopManager.GetCurrentItemCost(itemInstance.rarity);
        if (shopManager.TrySpendGold(cost))
            return true;

        LogDebug($"Compra cancelada: oro insuficiente. costo={cost}, oro={shopManager.CurrentGold}.");
        CancelDrag();
        return false;
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.LogWarning($"[DraggableShopItem] {message}", this);
    }

    private string DescribeItem()
    {
        if (itemInstance == null || itemInstance.data == null)
            return "<null>";

        return
            $"{itemInstance.data.id} rarity={itemInstance.rarity} " +
            $"side={itemInstance.crafterSide} placement={itemInstance.placementRule}";
    }

    private bool CanPlaceInAnvilSlot(AnvilSlotUI slot)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return false;

        if (slot == null || itemInstance == null)
            return false;

        return AnvilCraftManager.GetOrCreate().CanPlaceItemInSlot(itemInstance, slot);
    }

    private bool CanPlaceCarryInAnvilSlot(AnvilSlotUI slot)
    {
        if (AnvilCraftManager.IsResolvingCraftSequence)
            return false;

        if (slot == null || activeDraggedVisual == null || activeDraggedVisual.ItemInstance == null)
            return false;

        return AnvilCraftManager.GetOrCreate().CanPlaceItemInSlot(activeDraggedVisual.ItemInstance, slot);
    }

    private string DescribeDraggedVisual(DraggedItemVisual visual)
    {
        if (visual == null || visual.ItemInstance == null || visual.ItemInstance.data == null)
            return "<null>";

        return
            $"{visual.ItemInstance.data.id} rarity={visual.ItemInstance.rarity} " +
            $"side={visual.ItemInstance.crafterSide} placement={visual.ItemInstance.placementRule}";
    }

    private bool IsDraggedVisualAlive(DraggedItemVisual visual)
    {
        return visual != null && visual.gameObject != null;
    }

    private Vector2? GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return null;
#endif
    }

    private bool WasPrimaryPointerReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasReleasedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonUp(0);
#else
        return false;
#endif
    }

    private void UpdatePlacementPreview(Vector2 screenPosition)
    {
        if (activeDraggedVisual == null || EventSystem.current == null)
            return;

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        AnvilSlotUI anvilSlot = GetAnvilSlotUnderPointer(pointerEventData);
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

        bool canPlace = isHoldingDisplacedItem
            ? CanPlaceCarryInAnvilSlot(anvilSlot)
            : CanPlaceInAnvilSlot(anvilSlot);

        previewedAnvilSlot.ShowPlacementPreview(canPlace);
    }

    private void ClearAnvilPlacementPreview()
    {
        if (previewedAnvilSlot == null)
            return;

        previewedAnvilSlot.ClearPlacementPreview();
        previewedAnvilSlot = null;
    }
}
