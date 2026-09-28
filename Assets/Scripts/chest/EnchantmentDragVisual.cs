using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class EnchantmentDragVisual : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private EnchantmentInstance enchantment;
    private ChestSlotUI sourceSlot;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector2 originalOffsetMin;
    private Vector2 originalOffsetMax;
    private DraggedItemVisual hoveredItem;
    private bool dragging;

    public void Initialize(EnchantmentInstance data, ChestSlotUI slot)
    {
        enchantment = data;
        sourceSlot = slot;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (enchantment == null || sourceSlot == null)
            return;

        dragging = true;
        ItemTooltipUI.HideFor(sourceSlot);
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalPivot = rectTransform.pivot;
        originalOffsetMin = rectTransform.offsetMin;
        originalOffsetMax = rectTransform.offsetMax;

        Transform dragLayer = GetDragLayer();
        transform.SetParent(dragLayer, false);
        transform.SetAsLastSibling();
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(58f, 58f);
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one * 1.12f;
        canvasGroup.blocksRaycasts = false;
        UpdatePosition(eventData);
        UpdateHoveredItem(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            UpdatePosition(eventData);
            UpdateHoveredItem(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        DraggedItemVisual targetItem = FindItemUnderPointer(eventData);
        ClearHoveredItem();
        if (targetItem != null && targetItem.TryAddEnchantment(enchantment))
        {
            sourceSlot.RemoveEnchantment(enchantment);
            targetItem.PlayEnchantmentAppliedAnimation();
            Destroy(gameObject);
            return;
        }

        RestoreToSource();
    }

    private void UpdateHoveredItem(PointerEventData eventData)
    {
        DraggedItemVisual nextItem = FindItemUnderPointer(eventData);
        if (nextItem == hoveredItem)
            return;

        ClearHoveredItem();
        hoveredItem = nextItem;
        if (hoveredItem != null)
            hoveredItem.SetEnchantmentDropHover(true);
    }

    private void ClearHoveredItem()
    {
        if (hoveredItem != null)
            hoveredItem.SetEnchantmentDropHover(false);
        hoveredItem = null;
    }

    private DraggedItemVisual FindItemUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return null;

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            DraggedItemVisual item = results[i].gameObject.GetComponentInParent<DraggedItemVisual>();
            if (item != null)
                return item;
        }

        return null;
    }

    private void UpdatePosition(PointerEventData eventData)
    {
        RectTransform dragArea = rectTransform.parent as RectTransform;
        if (dragArea == null)
            return;

        Canvas dragCanvas = dragArea.GetComponentInParent<Canvas>();
        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && dragCanvas != null && dragCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = dragCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragArea, eventData.position, eventCamera, out Vector2 point))
            rectTransform.anchoredPosition = point;
    }

    private Transform GetDragLayer()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            return transform.parent;

        Canvas rootCanvas = canvas.rootCanvas;
        Transform dragLayer = rootCanvas.transform.Find("DragLayer");
        return dragLayer != null ? dragLayer : rootCanvas.transform;
    }

    private void RestoreToSource()
    {
        ClearHoveredItem();
        if (originalParent == null)
            return;

        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, originalParent.childCount - 1));
        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.pivot = originalPivot;
        rectTransform.offsetMin = originalOffsetMin;
        rectTransform.offsetMax = originalOffsetMax;
        rectTransform.localScale = Vector3.one;
        canvasGroup.blocksRaycasts = true;
    }
}
