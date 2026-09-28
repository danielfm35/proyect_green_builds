using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class ModifierDragVisual : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ModifierInstance modifier;
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

    public void Initialize(ModifierInstance data, ChestSlotUI slot)
    {
        modifier = data;
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
        if (modifier == null || modifier.data == null || sourceSlot == null)
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
        rectTransform.sizeDelta = new Vector2(64f, 64f);
        rectTransform.localScale = Vector3.one * 1.15f;
        rectTransform.localRotation = Quaternion.identity;
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
        if (!dragging) return;
        dragging = false;

        DraggedItemVisual target = FindItemUnderPointer(eventData);
        ClearHoveredItem();
        if (target != null && target.ApplyModifier(modifier))
        {
            sourceSlot.RemoveModifier(modifier);
            target.PlayModifierAppliedAnimation();
            Destroy(gameObject);
            return;
        }

        Restore();
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
        if (EventSystem.current == null) return null;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            DraggedItemVisual item = results[i].gameObject.GetComponentInParent<DraggedItemVisual>();
            if (item != null) return item;
        }
        return null;
    }

    private void UpdatePosition(PointerEventData eventData)
    {
        RectTransform area = rectTransform.parent as RectTransform;
        if (area == null) return;
        Canvas dragCanvas = area.GetComponentInParent<Canvas>();
        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && dragCanvas != null && dragCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = dragCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventCamera, out Vector2 point))
            rectTransform.anchoredPosition = point;
    }

    private Transform GetDragLayer()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return transform.parent;
        Canvas root = canvas.rootCanvas;
        Transform layer = root.transform.Find("DragLayer");
        return layer != null ? layer : root.transform;
    }

    private void Restore()
    {
        ClearHoveredItem();
        if (originalParent == null) return;
        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, originalParent.childCount - 1));
        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.pivot = originalPivot;
        rectTransform.offsetMin = originalOffsetMin;
        rectTransform.offsetMax = originalOffsetMax;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        canvasGroup.blocksRaycasts = true;
    }
}
